using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using GitTfs.Core;
using StructureMap;
using GitTfs.Util;
using GitTfs.Core.TfsInterop;

namespace GitTfs.Commands
{
    [Pluggable("clone")]
    [Description("clone [options] tfs-url-or-instance-name repository-path <git-repository-path>\n  ex : git tfs clone http://myTfsServer:8080/tfs/TfsRepository $/ProjectName/ProjectBranch\n")]
    public class Clone : GitTfsCommand
    {
        private readonly Fetch _fetch;
        private readonly Init _init;
        private readonly Globals _globals;
        private readonly InitBranch _initBranch;
        private bool _resumable;

        public Clone(Globals globals, Fetch fetch, Init init, InitBranch initBranch)
        {
            _fetch = fetch;
            _init = init;
            _globals = globals;
            _initBranch = initBranch;
            globals.GcCountdown = globals.GcPeriod;
        }

        public OptionSet OptionSet
        {
            get
            {
                return _init.OptionSet.Merge(_fetch.OptionSet)
                           .Add("resumable", "if an error occurred, try to continue when you restart clone with same parameters", v => _resumable = v != null);
            }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            string gitRepositoryPath;
            if (tfsRepositoryPath == GitTfsConstants.TfsRoot)
                gitRepositoryPath = "tfs-collection";
            else
                gitRepositoryPath = Path.GetFileName(tfsRepositoryPath);
            return Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);
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
                    retVal = _init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);
                }
                else
                {
                    try
                    {
                        Environment.CurrentDirectory = gitRepositoryPath;
                        _globals.Repository = _init.GitHelper.MakeRepository(_globals.GitDir);
                    }
                    catch (Exception)
                    {
                        retVal = _init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);
                    }
                }

                VerifyTfsPathToClone(tfsRepositoryPath);
            }
            catch
            {
                if (!_resumable)
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
                        string msg = string.Format("warning: Something went wrong while cleaning file after internal error (See below).\n    Can't clean up files because of IOException:\n{0}\n", e.IndentExceptionMessage());
                        Trace.WriteLine(msg);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        // swallow it also
                        string msg = string.Format("warning: Something went wrong while cleaning file after internal error (See below).\n    Can't clean up files because of UnauthorizedAccessException:\n{0}\n", e.IndentExceptionMessage());
                        Trace.WriteLine(msg);
                    }
                }

                throw;
            }
            bool errorOccurs = false;
            try
            {
                if (tfsRepositoryPath == GitTfsConstants.TfsRoot)
                    _fetch.BranchStrategy = BranchStrategy.None;

                _globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, _fetch.BranchStrategy == BranchStrategy.None);

                if (retVal == 0)
                {
                    _fetch.Run(_fetch.BranchStrategy == BranchStrategy.All);
                    _globals.Repository.GarbageCollect();
                }

                if (_fetch.BranchStrategy == BranchStrategy.All && _initBranch != null)
                {
                    _initBranch.CloneAllBranches = true;

                    retVal = _initBranch.Run();
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
                    + (_fetch.BranchStrategy == BranchStrategy.All ? "branch --init --all" : "fetch") + "`\n", ex);
            }
            finally
            {
                try
                {
                    if (!_init.IsBare) _globals.Repository.Merge(_globals.Repository.ReadTfsRemote(_globals.RemoteId).RemoteRef);
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
            if (_initBranch == null)
                return;
            try
            {
                var remote = _globals.Repository.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);

                if (!remote.Tfs.IsExistingInTfs(tfsRepositoryPath))
                    throw new GitTfsException("error: the path " + tfsRepositoryPath + " you want to clone doesn't exist!")
                        .WithRecommendation("To discover which branch to clone, you could use the command :\ngit tfs list-remote-branches " + remote.TfsUrl);

                if (_fetch.BranchStrategy == BranchStrategy.None)
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

                    if (_fetch.BranchStrategy == BranchStrategy.All)
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
                if (!isDebuggerAttached && !_resumable)
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
