using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;
using Sep.Git.Tfs.Util;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("clone")]
    [Description("clone [options] tfs-url-or-instance-name repository-path <git-repository-path>\n  ex : git tfs clone http://myTfsServer:8080/tfs/TfsRepository $/ProjectName/ProjectBranch\n")]
    public class Clone : GitTfsCommand
    {
        private readonly Fetch fetch;
        private readonly Init init;
        private readonly Globals globals;
        private readonly InitBranch initBranch;
        private bool withBranches;
        private TextWriter stdout;

        public Clone(Globals globals, Fetch fetch, Init init, InitBranch initBranch, TextWriter stdout)
        {
            this.fetch = fetch;
            this.init = init;
            this.globals = globals;
            this.initBranch = initBranch;
            //[Temporary] Remove in the next version!
            if (initBranch != null)
                initBranch.DontDisplayObsoleteMessage = true;
            this.stdout = stdout;
        }

        public CancellationToken Token { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return init.OptionSet.Merge(fetch.OptionSet)
                           .Add("with-branches", "init all the TFS branches during the clone", v => withBranches = v != null);
            }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            return Run(tfsUrl, tfsRepositoryPath, Path.GetFileName(tfsRepositoryPath));
        }

        public int Run(string tfsUrl, string tfsRepositoryPath, string gitRepositoryPath)
        {
            var currentDir = Environment.CurrentDirectory;
            var repositoryDirCreated = InitGitDir(gitRepositoryPath);

            // TFS string representations of repository paths do not end in trailing slashes
            tfsRepositoryPath = (tfsRepositoryPath ?? string.Empty).TrimEnd('/');

            int retVal;
            try
            {
                retVal = init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);

                VerifyTfsPathToClone(tfsRepositoryPath);

                if (retVal == 0) fetch.Run(withBranches);
            }
            catch
            {
                try
                {
                    // if we appeared to be inside repository dir when exception was thrown - we won't be able to delete it
                    Environment.CurrentDirectory = currentDir;
                    if (repositoryDirCreated)
                        Directory.Delete(gitRepositoryPath, recursive: true);
                    else
                        CleanDirectory(gitRepositoryPath);
                }
                catch (IOException e)
                {
                    // swallow IOException. Smth went wrong before this and we're much more interested in that error
                    string msg = String.Format("warning: Something went wrong while cleaning file after internal error (See below).\n    Can't cleanup files because of IOException:\n{0}\n", e.IndentExceptionMessage());
                    Trace.WriteLine(msg);
                }
                catch (UnauthorizedAccessException e)
                {
                    // swallow it also
                    string msg = String.Format("warning: Something went wrong while cleaning file after internal error (See below).\n    Can't cleanup files because of UnauthorizedAccessException:\n{0}\n", e.IndentExceptionMessage());
                    Trace.WriteLine(msg);
                }

                throw;
            }
            if (withBranches && initBranch != null)
            {
                initBranch.CloneAllBranches = true;
                retVal = initBranch.Run();
            }
            if (!init.IsBare) globals.Repository.CommandNoisy("merge", globals.Repository.ReadTfsRemote(globals.RemoteId).RemoteRef);
            return retVal;
        }

        private void VerifyTfsPathToClone(string tfsRepositoryPath)
        {
            if (initBranch != null)
            {
                var remote = globals.Repository.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);

                if (!remote.Tfs.IsExistingInTfs(tfsRepositoryPath))
                    throw new GitTfsException("error: the path " + tfsRepositoryPath + " you want to clone doesn't exist!")
                        .WithRecommendation("To discover which branch to clone, you could use the command :\ngit tfs list-remote-branches " + remote.TfsUrl);

                if (!remote.Tfs.CanGetBranchInformation)
                    return;
                var tfsTrunkRepository = remote.Tfs.GetRootTfsBranchForRemotePath(tfsRepositoryPath, false);
                if (tfsTrunkRepository == null)
                {
                    var tfsRootBranches = remote.Tfs.GetAllTfsRootBranchesOrderedByCreation();
                    if (!tfsRootBranches.Any())
                    {
                        stdout.WriteLine("info: no TFS root found !\n\nPS:perhaps you should convert your trunk folder into a branch in TFS.");
                        return;
                    }
                    var cloneMsg = "   => If you want to manage branches with git-tfs, clone one of this branch instead :\n"
                                    + " - " + tfsRootBranches.Aggregate((s1, s2) => s1 + "\n - " + s2)
                                    + "\n\nPS:if your branch is not listed here, perhaps you should convert the containing folder to a branch in TFS.";
                    
                    if (withBranches)
                        throw new GitTfsException("error: cloning the whole repository or too high in the repository path doesn't permit to manage branches!\n" + cloneMsg);
                    stdout.WriteLine("warning: you are going to clone the whole repository or too high in the repository path !\n" + cloneMsg);
                    return;
                }

                var tfsBranchesPath = tfsTrunkRepository.GetAllChildren();
                var tfsPathToClone = tfsRepositoryPath.TrimEnd('/').ToLower();
                var tfsTrunkRepositoryPath = tfsTrunkRepository.Path;
                if (tfsPathToClone != tfsTrunkRepositoryPath.ToLower())
                {
                    if (tfsBranchesPath.Select(e=>e.Path.ToLower()).Contains(tfsPathToClone))
                        stdout.WriteLine("info: you are going to clone a branch instead of the trunk ( {0} )\n"
                            + "   => If you want to manage branches with git-tfs, clone {0} with '--with-branches' option instead...)", tfsTrunkRepositoryPath);
                    else
                        stdout.WriteLine("warning: you are going to clone a subdirectory of a branch and won't be able to manage branches :(\n"
                            + "   => If you want to manage branches with git-tfs, clone " + tfsTrunkRepositoryPath + " with '--with-branches' option instead...)");
                }
            }
        }

        private static bool InitGitDir(string gitRepositoryPath)
        {
            bool repositoryDirCreated = false;
            var di = new DirectoryInfo(gitRepositoryPath);
            if (di.Exists)
            {
                if (di.EnumerateFileSystemInfos().Any())
                    throw new GitTfsException("error: Specified git repository directory is not empty");
            }
            else
            {
                repositoryDirCreated = true;
                di.Create();
            }
            return repositoryDirCreated;
        }

        private static void CleanDirectory(string gitRepositoryPath)
        {
            var di = new DirectoryInfo(gitRepositoryPath);
            foreach (var fileSystemInfo in di.EnumerateDirectories())
                fileSystemInfo.Delete(true);
            foreach (var fileSystemInfo in di.EnumerateFiles())
                fileSystemInfo.Delete();
        }
    }
}
