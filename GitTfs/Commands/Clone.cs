using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private bool resumable;

        public Clone(Globals globals, Fetch fetch, Init init, InitBranch initBranch)
        {
            this.fetch = fetch;
            this.init = init;
            this.globals = globals;
            this.initBranch = initBranch;
            globals.GcCountdown = globals.GcPeriod;
        }

        public OptionSet OptionSet
        {
            get
            {
                return init.OptionSet.Merge(fetch.OptionSet)
                           .Add("resumable", "if an error occurred, try to continue when you restart clone with same parameters", v => resumable = v != null);
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
            if (tfsRepositoryPath != GitTfsConstants.TfsRoot)
                tfsRepositoryPath = (tfsRepositoryPath ?? string.Empty).TrimEnd('/');

            int retVal = 0;
            try
            {
                if (repositoryDirCreated)
                {
                    retVal = init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);
                }
                else
                {
                    try
                    {
                        Environment.CurrentDirectory = gitRepositoryPath;
                        globals.Repository = init.GitHelper.MakeRepository(globals.GitDir);
                    }
                    catch (Exception)
                    {
                        retVal = init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);
                    }
                }

                VerifyTfsPathToClone(tfsRepositoryPath);
            }
            catch
            {
                if (!resumable)
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
                        string msg = String.Format("warning: Something went wrong while cleaning file after internal error (See below).\n    Can't clean up files because of IOException:\n{0}\n", e.IndentExceptionMessage());
                        Trace.WriteLine(msg);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        // swallow it also
                        string msg = String.Format("warning: Something went wrong while cleaning file after internal error (See below).\n    Can't clean up files because of UnauthorizedAccessException:\n{0}\n", e.IndentExceptionMessage());
                        Trace.WriteLine(msg);
                    }
                }

                throw;
            }
            bool errorOccurs = false;
            try
            {
                if (tfsRepositoryPath == GitTfsConstants.TfsRoot)
                    fetch.BranchStrategy = BranchStrategy.None;

                globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, (fetch.BranchStrategy == BranchStrategy.None).ToString());

                if (retVal == 0)
                {
                    fetch.Run(fetch.BranchStrategy == BranchStrategy.All);
                    globals.Repository.GarbageCollect();
                }

                if (fetch.BranchStrategy == BranchStrategy.All && initBranch != null)
                {
                    initBranch.CloneAllBranches = true;

                    retVal = initBranch.Run();
                }
            }
            catch (GitTfsException)
            {
                errorOccurs = true;
                throw;
            }
            catch (Exception ex)
            {
                errorOccurs = true;
                throw new GitTfsException("error: a problem occurred when trying to clone the repository. Try to solve the problem described below.\nIn any case, after, try to continue using command `git tfs "
                    + (fetch.BranchStrategy == BranchStrategy.All ? "branch init --all" : "fetch") + "`\n", ex);
            }
            finally
            {
                try
                {
                    if (!init.IsBare) globals.Repository.Merge(globals.Repository.ReadTfsRemote(globals.RemoteId).RemoteRef);
                }
                catch (Exception)
                {
                    //Swallow exception because the previously thrown exception is more important...
                    if (!errorOccurs)
                        throw;
                }
            }
            return retVal;
        }

        private void VerifyTfsPathToClone(string tfsRepositoryPath)
        {
            if (initBranch == null)
                return;
            try
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
                        Trace.TraceInformation("info: no TFS root found !\n\nPS:perhaps you should convert your trunk folder into a branch in TFS.");
                        return;
                    }
                    var cloneMsg = "   => If you want to manage branches with git-tfs, clone one of this branch instead :\n"
                                    + " - " + tfsRootBranches.Aggregate((s1, s2) => s1 + "\n - " + s2)
                                    + "\n\nPS:if your branch is not listed here, perhaps you should convert the containing folder to a branch in TFS.";

                    if (fetch.BranchStrategy == BranchStrategy.All)
                        throw new GitTfsException("error: cloning the whole repository or too high in the repository path doesn't permit to manage branches!\n" + cloneMsg);
                    Trace.TraceWarning("warning: you are going to clone the whole repository or too high in the repository path !\n" + cloneMsg);
                    return;
                }

                var tfsBranchesPath = tfsTrunkRepository.GetAllChildren();
                var tfsPathToClone = tfsRepositoryPath.TrimEnd('/').ToLower();
                var tfsTrunkRepositoryPath = tfsTrunkRepository.Path;
                if (tfsPathToClone != tfsTrunkRepositoryPath.ToLower())
                {
                    if (tfsBranchesPath.Select(e => e.Path.ToLower()).Contains(tfsPathToClone))
                        Trace.TraceInformation("info: you are going to clone a branch instead of the trunk ( {0} )\n"
                            + "   => If you want to manage branches with git-tfs, clone {0} with '--branches=all' option instead...)", tfsTrunkRepositoryPath);
                    else
                        Trace.TraceWarning("warning: you are going to clone a subdirectory of a branch and won't be able to manage branches :(\n"
                            + "   => If you want to manage branches with git-tfs, clone " + tfsTrunkRepositoryPath + " with '--branches=all' option instead...)");
                }
            }
            catch (GitTfsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("warning: a server error occurs when trying to verify the tfs path cloned:\n   " + ex.Message
                    + "\n   try to continue anyway...");
            }
        }

        private bool InitGitDir(string gitRepositoryPath)
        {
            bool repositoryDirCreated = false;
            var di = new DirectoryInfo(gitRepositoryPath);
            if (di.Exists)
            {
                bool isDebuggerAttached = false;
#if DEBUG
                isDebuggerAttached = Debugger.IsAttached;
#endif
                if (!isDebuggerAttached && !resumable)
                {
                    if (di.EnumerateFileSystemInfos().Any())
                        throw new GitTfsException("error: Specified git repository directory is not empty");
                }
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
