using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("clone")]
    [Description("clone [options] tfs-url-or-instance-name repository-path <git-repository-path>")]
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
            this.stdout = stdout;
        }

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

            try
            {
                var retVal = 0;
                retVal = init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);

                VerifyTfsPathToClone(tfsRepositoryPath);

                if (retVal == 0) retVal = fetch.Run();
                if (retVal == 0) globals.Repository.CommandNoisy("merge", globals.Repository.ReadAllTfsRemotes().First().RemoteRef);
                if (retVal == 0 && withBranches && initBranch != null)
                {
                    initBranch.CloneAllBranches = true;
                    retVal = initBranch.Run();
                }
                return retVal;
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
                    string msg = String.Format("Something went wrong, can't cleanup files because of IOException:\n{0}\n", e.IndentExceptionMessage());
                    Trace.WriteLine(msg);
                }
                catch (UnauthorizedAccessException e)
                {
                    // swallow it also
                    string msg = String.Format("Something went wrong, can't cleanup files because of UnauthorizedAccessException:\n{0}\n", e.IndentExceptionMessage());
                    Trace.WriteLine(msg);
                }

                throw;
            }
        }

        private void VerifyTfsPathToClone(string tfsRepositoryPath)
        {
            if (initBranch != null)
            {
                var remote = globals.Repository.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);
                List<string> tfsBranchesPath;
                try
                {
                    tfsBranchesPath = remote.Tfs.GetAllTfsBranchesOrderedByCreation().ToList();
                }
                //Catch GitTfsException for TFS2008 where GetAllTfsBranchesOrderedByCreation() is not supported/implemented
                //=>No control could be done!
                catch (GitTfsException) { return; }
                var tfsPathToClone = tfsRepositoryPath.TrimEnd('/').ToLower();
                var tfsTrunkRepositoryPath = tfsBranchesPath.First();
                if (tfsPathToClone != tfsTrunkRepositoryPath.ToLower())
                {
                    if (tfsBranchesPath.Select(e=>e.ToLower()).Contains(tfsPathToClone))
                        stdout.WriteLine("info: you are going to clone a branch instead of the trunk ( {0} )\n"
                            + "   => If you want to manage branches with git-tfs, clone {0} with '--with-branches' option instead...)", tfsTrunkRepositoryPath);
                    else
                    {
                        if (tfsTrunkRepositoryPath.ToLower().IndexOf(tfsPathToClone) == 0)
                        {
                            if (withBranches)
                                throw new GitTfsException("error: cloning the whole repository doesn't permit to manage branches!\n"
                                    + "   =>If you want to manage branches with git-tfs, clone " + tfsTrunkRepositoryPath + " instead...");
                            stdout.WriteLine("warning: you are going to clone the whole repository!\n"
                                + "   =>If you want to manage branches with git-tfs, clone " + tfsTrunkRepositoryPath + " instead...");
                        }
                        else
                        {
                            stdout.WriteLine("warning: you are going to clone a subdirectory of a branch and won't be able to manage branches :(\n"
                                + "   => If you want to manage branches with git-tfs, clone " + tfsTrunkRepositoryPath + " with '--with-branches' option instead...)");
                        }
                    }
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
