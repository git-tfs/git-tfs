using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NDesk.Options;
using GitTfs.Core;
using GitTfs.Util;
using GitTfs.Core.TfsInterop;

namespace GitTfs.Commands
{
    public class InitBranch : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly Help _helper;

        private RemoteOptions _remoteOptions;
        public string TfsUsername { get; set; }
        public string TfsPassword { get; set; }
        public string IgnoreRegex { get; set; }
        public string ExceptRegex { get; set; }
        public bool CloneAllBranches { get; set; }
        public bool NoFetch { get; set; }
        public bool DontCreateGitBranch { get; set; }

        public IGitTfsRemote RemoteCreated { get; private set; }

        public InitBranch(Globals globals, Help helper, AuthorsFile authors)
        {
            _globals = globals;
            _helper = helper;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "all", "Clone all the TFS branches (For TFS 2010 and later)", v => CloneAllBranches = (v.ToLower() == "all") },
                    { "u|username=", "TFS username", v => TfsUsername = v },
                    { "p|password=", "TFS password", v => TfsPassword = v },
                    { "ignore-regex=", "A regex of files to ignore", v => IgnoreRegex = v },
                    { "except-regex=", "A regex of exceptions to ignore-regex", v => ExceptRegex = v},
                    { "no-fetch", "Create the new TFS remote but don't fetch any changesets", v => NoFetch = (v != null) }
                };
            }
        }

        public int Run()
        {
            return Run(null, null);
        }

        public int Run(string tfsBranchPath)
        {
            return Run(tfsBranchPath, null);
        }

        public int Run(string tfsBranchPath, string gitBranchNameExpected)
        {
            if (!CloneAllBranches && tfsBranchPath == null)
            {
                _helper.Run(this);
                return GitTfsExitCodes.Help;
            }

            if (CloneAllBranches)
            {
                return CloneAll(tfsBranchPath);
            }
            else
            {
                return CloneBranch(tfsBranchPath, gitBranchNameExpected);
            }
        }

        private int CloneBranch(string tfsBranchPath, string gitBranchNameExpected)
        {
            var defaultRemote = InitFromDefaultRemote();

            // TFS representations of repository paths do not have trailing slashes
            tfsBranchPath = (tfsBranchPath ?? string.Empty).TrimEnd('/');

            if (!tfsBranchPath.IsValidTfsPath())
            {
                var remotes = _globals.Repository.GetLastParentTfsCommits(tfsBranchPath);
                if (!remotes.Any())
                {
                    throw new Exception("error: No TFS branch found!");
                }
                tfsBranchPath = remotes.First().Remote.TfsRepositoryPath;
            }
            tfsBranchPath.AssertValidTfsPath();

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();
            var remote = allRemotes.FirstOrDefault(r => r.TfsRepositoryPath.ToLower() == tfsBranchPath.ToLower());
            if (remote != null && remote.MaxChangesetId != 0)
            {
                Trace.TraceInformation("warning : There is already a remote for this tfs branch. Branch ignored!");
                return GitTfsExitCodes.InvalidArguments;
            }

            IList<RootBranch> creationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath);

            IFetchResult fetchResult;
            InitBranchSupportingRename(tfsBranchPath, gitBranchNameExpected, creationBranchData, defaultRemote, out fetchResult);
            return GitTfsExitCodes.OK;
        }

        private IGitTfsRemote InitBranchSupportingRename(string tfsBranchPath, string gitBranchNameExpected, IList<RootBranch> creationBranchData, IGitTfsRemote defaultRemote, out IFetchResult fetchResult)
        {
            fetchResult = null;

            RemoveAlreadyFetchedBranches(creationBranchData, defaultRemote);

            Trace.TraceInformation("Branches to Initialize successively :");
            foreach (var branch in creationBranchData)
                Trace.TraceInformation("-" + branch.TfsBranchPath + " (" + branch.SourceBranchChangesetId + ")");

            IGitTfsRemote branchTfsRemote = null;
            var remoteToDelete = new List<IGitTfsRemote>();
            foreach (var rootBranch in creationBranchData)
            {
                Trace.WriteLine("Processing " + (rootBranch.IsRenamedBranch ? "renamed " : string.Empty) + "branch :"
                    + rootBranch.TfsBranchPath + " (" + rootBranch.SourceBranchChangesetId + ")");
                var cbd = new BranchCreationDatas() { RootChangesetId = rootBranch.SourceBranchChangesetId, TfsRepositoryPath = rootBranch.TfsBranchPath };
                if (cbd.TfsRepositoryPath == tfsBranchPath)
                    cbd.GitBranchNameExpected = gitBranchNameExpected;

                branchTfsRemote = defaultRemote.InitBranch(_remoteOptions, cbd.TfsRepositoryPath, cbd.RootChangesetId, !NoFetch, cbd.GitBranchNameExpected, fetchResult);
                if (branchTfsRemote == null)
                {
                    throw new GitTfsException("error: Couldn't fetch parent branch\n");
                }

                // If this branch's branch point is past the first commit, indicate this so Fetch can start from that point
                if (rootBranch.TargetBranchChangesetId > -1)
                {
                    branchTfsRemote.SetFirstChangeset(rootBranch.TargetBranchChangesetId);
                }

                if (rootBranch.IsRenamedBranch || !NoFetch)
                {
                    fetchResult = FetchRemote(branchTfsRemote, false, !DontCreateGitBranch && !rootBranch.IsRenamedBranch, fetchResult, rootBranch.TargetBranchChangesetId);
                    if (fetchResult.IsSuccess && rootBranch.IsRenamedBranch)
                        remoteToDelete.Add(branchTfsRemote);
                }
                else
                    Trace.WriteLine("Not fetching changesets, --no-fetch option specified");
            }
            foreach (var gitTfsRemote in remoteToDelete)
            {
                _globals.Repository.DeleteTfsRemote(gitTfsRemote);
            }
            return RemoteCreated = branchTfsRemote;
        }

        private static void RemoveAlreadyFetchedBranches(IList<RootBranch> creationBranchData, IGitTfsRemote defaultRemote)
        {
            for (int i = creationBranchData.Count - 1; i > 0; i--)
            {
                var branch = creationBranchData[i];
                if (defaultRemote.Repository.FindCommitHashByChangesetId(branch.SourceBranchChangesetId) != null)
                {
                    for (int j = 0; j < i; j++)
                    {
                        creationBranchData.RemoveAt(0);
                    }
                    break;
                }
            }
        }

        private class BranchCreationDatas
        {
            public string TfsRepositoryPath { get; set; }
            public string GitBranchNameExpected { get; set; }
            public int RootChangesetId { get; set; }
        }

        [DebuggerDisplay("{TfsRepositoryPath} C{RootChangesetId}")]
        private class BranchDatas
        {
            public string TfsRepositoryPath { get; set; }
            public IGitTfsRemote TfsRemote { get; set; }
            public bool IsEntirelyFetched { get; set; }
            public int RootChangesetId { get; set; }
            public IList<RootBranch> CreationBranchData { get; set; }
            public Exception Error { get; set; }
        }

        private int CloneAll(string gitRemote)
        {
            if (CloneAllBranches && NoFetch)
                throw new GitTfsException("error: --no-fetch cannot be used with --all");

            _globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, false);

            var defaultRemote = InitFromDefaultRemote();

            List<BranchDatas> childBranchesToInit;
            if (gitRemote == null)
            {
                childBranchesToInit = GetChildBranchesToInit(defaultRemote);
            }
            else
            {
                var gitRepositoryBranchRemotes = defaultRemote.Repository.GetGitRemoteBranches(gitRemote);
                var childBranchPaths = new Dictionary<string, BranchDatas>();
                foreach (var branchRemote in gitRepositoryBranchRemotes)
                {
                    var branchRemoteChangesetInfos = _globals.Repository.GetLastParentTfsCommits(branchRemote);
                    var firstRemoteChangesetInfo = branchRemoteChangesetInfos.FirstOrDefault();

                    if (firstRemoteChangesetInfo == null)
                    {
                        continue;
                    }

                    var branchRemoteTfsPath = firstRemoteChangesetInfo.Remote.TfsRepositoryPath;

                    if (!childBranchPaths.ContainsKey(branchRemoteTfsPath))
                        childBranchPaths.Add(branchRemoteTfsPath, new BranchDatas { TfsRepositoryPath = branchRemoteTfsPath });
                }

                childBranchesToInit = childBranchPaths.Values.ToList();
            }

            if (!childBranchesToInit.Any())
            {
                Trace.TraceInformation("No other Tfs branches found.");
            }
            else
            {
                return InitializeBranches(defaultRemote, childBranchesToInit) ? GitTfsExitCodes.OK : GitTfsExitCodes.SomeDataCouldNotHaveBeenRetrieved;
            }

            return GitTfsExitCodes.OK;
        }

        private static List<BranchDatas> GetChildBranchesToInit(IGitTfsRemote defaultRemote)
        {
            var rootBranch = defaultRemote.Tfs.GetRootTfsBranchForRemotePath(defaultRemote.TfsRepositoryPath);
            if (rootBranch == null)
                throw new GitTfsException("error: The use of the option '--branches=all' to init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '"
                    + defaultRemote.TfsRepositoryPath + "' is not a TFS branch!");

            return rootBranch.GetAllChildrenOfBranch(defaultRemote.TfsRepositoryPath).Select(b => new BranchDatas { TfsRepositoryPath = b.Path }).ToList();
        }

        private bool InitializeBranches(IGitTfsRemote defaultRemote, List<BranchDatas> childBranchPaths)
        {
            Trace.TraceInformation("Tfs branches found:");
            var branchesToProcess = new List<BranchDatas>();
            foreach (var childBranchPath in childBranchPaths)
            {
                Trace.TraceInformation("- " + childBranchPath.TfsRepositoryPath);
                var branchDatas = new BranchDatas
                {
                    TfsRepositoryPath = childBranchPath.TfsRepositoryPath,
                    TfsRemote = _globals.Repository.ReadAllTfsRemotes().FirstOrDefault(r => r.TfsRepositoryPath == childBranchPath.TfsRepositoryPath)
                };
                try
                {
                    branchDatas.CreationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(childBranchPath.TfsRepositoryPath);
                }
                catch (Exception ex)
                {
                    branchDatas.Error = ex;
                }

                branchesToProcess.Add(branchDatas);
            }
            branchesToProcess.Add(new BranchDatas { TfsRepositoryPath = defaultRemote.TfsRepositoryPath, TfsRemote = defaultRemote, RootChangesetId = -1 });

            bool isSomethingDone;
            do
            {
                isSomethingDone = false;
                var branchesToFetch = branchesToProcess.Where(b => !b.IsEntirelyFetched && b.Error == null).ToList();
                foreach (var tfsBranch in branchesToFetch)
                {
                    Trace.TraceInformation("=> Working on TFS branch : " + tfsBranch.TfsRepositoryPath);
                    if (tfsBranch.TfsRemote == null || tfsBranch.TfsRemote.MaxChangesetId == 0)
                    {
                        try
                        {
                            IFetchResult fetchResult;
                            tfsBranch.TfsRemote = InitBranchSupportingRename(tfsBranch.TfsRepositoryPath, null, tfsBranch.CreationBranchData, defaultRemote,
                                                                             out fetchResult);
                            if (tfsBranch.TfsRemote != null)
                            {
                                tfsBranch.IsEntirelyFetched = fetchResult.IsSuccess;
                                isSomethingDone = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation("error: an error occurs when initializing the branch. Branch is ignored and continuing...");
                            tfsBranch.Error = ex;
                        }
                    }
                    else
                    {
                        try
                        {
                            var lastFetchedChangesetId = tfsBranch.TfsRemote.MaxChangesetId;
                            Trace.WriteLine("Fetching remote :" + tfsBranch.TfsRemote.Id);
                            var fetchResult = FetchRemote(tfsBranch.TfsRemote, true);
                            tfsBranch.IsEntirelyFetched = fetchResult.IsSuccess;
                            if (fetchResult.NewChangesetCount != 0)
                                isSomethingDone = true;
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceInformation("error: an error occurs when fetching changeset. Fetching is stopped and continuing...");
                            tfsBranch.Error = ex;
                        }
                    }
                }
            } while (branchesToProcess.Any(b => !b.IsEntirelyFetched && b.Error == null) && isSomethingDone);

            _globals.Repository.GarbageCollect();

            bool success = true;
            if (branchesToProcess.Any(b => !b.IsEntirelyFetched))
            {
                success = false;
                Trace.TraceWarning("warning: Some Tfs branches could not have been initialized:");
                foreach (var branchNotInited in branchesToProcess.Where(b => !b.IsEntirelyFetched))
                {
                    Trace.TraceInformation("- " + branchNotInited.TfsRepositoryPath);
                }
                Trace.TraceInformation("\nPlease report this case to the git-tfs developers! (report here : https://github.com/git-tfs/git-tfs/issues/461 )");
            }
            if (branchesToProcess.Any(b => b.Error != null))
            {
                success = false;
                Trace.TraceWarning("warning: Some Tfs branches could not have been initialized or entirely fetched due to errors:");
                foreach (var branchWithErrors in branchesToProcess.Where(b => b.Error != null))
                {
                    Trace.TraceInformation("- " + branchWithErrors.TfsRepositoryPath);
                    if (_globals.DebugOutput)
                        Trace.WriteLine("   =>error:" + branchWithErrors.Error);
                    else
                        Trace.TraceInformation("   =>error:" + branchWithErrors.Error.Message);
                }
                Trace.TraceInformation("\nPlease report this case to the git-tfs developers! (report here : https://github.com/git-tfs/git-tfs/issues )");
                return false;
            }

            return success;
        }

        private IGitTfsRemote InitFromDefaultRemote()
        {
            IGitTfsRemote defaultRemote;
            if (_globals.Repository.HasRemote(GitTfsConstants.DefaultRepositoryId))
                defaultRemote = _globals.Repository.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);
            else
                defaultRemote = _globals.Repository.ReadAllTfsRemotes()
                    .Where(x => x != null && x.RemoteInfo != null && !string.IsNullOrEmpty(x.RemoteInfo.Url))
                    .OrderBy(x => x.RemoteInfo.Url.Length).FirstOrDefault();
            if (defaultRemote == null)
                throw new GitTfsException("error: No git-tfs repository found. Please try to clone first...\n");

            _remoteOptions = new RemoteOptions();
            if (!string.IsNullOrWhiteSpace(TfsUsername))
            {
                _remoteOptions.Username = TfsUsername;
                _remoteOptions.Password = TfsPassword;
            }
            else
            {
                _remoteOptions.Username = defaultRemote.TfsUsername;
                _remoteOptions.Password = defaultRemote.TfsPassword;
            }

            if (IgnoreRegex != null)
                _remoteOptions.IgnoreRegex = IgnoreRegex;
            else
                _remoteOptions.IgnoreRegex = defaultRemote.IgnoreRegexExpression;

            if (ExceptRegex != null)
                _remoteOptions.ExceptRegex = ExceptRegex;
            else
                _remoteOptions.ExceptRegex = defaultRemote.IgnoreExceptRegexExpression;

            return defaultRemote;
        }

        /// <summary>
        /// Fetch changesets from <paramref name="tfsRemote"/>, optionally stopping on failed merge commits.
        /// </summary>
        /// <param name="tfsRemote">The TFS Remote from which to fetch changesets.</param>
        /// <param name="stopOnFailMergeCommit">
        /// Indicates whether to stop fetching when encountering a failed merge commit.
        /// </param>
        /// <param name="createBranch">
        /// If <c>true</c>, create a local Git branch starting from the earliest possible changeset in the given
        /// <paramref name="tfsRemote"/>.
        /// </param>
        /// <param name="renameResult"></param>
        /// <param name="startingChangesetId"></param>
        /// <returns>A <see cref="IFetchResult"/>.</returns>
        private IFetchResult FetchRemote(IGitTfsRemote tfsRemote, bool stopOnFailMergeCommit, bool createBranch = true, IRenameResult renameResult = null, int startingChangesetId = -1)
        {
            try
            {
                Trace.WriteLine("Try fetching changesets...");
                var fetchResult = tfsRemote.Fetch(stopOnFailMergeCommit, renameResult: renameResult);
                Trace.WriteLine("Changesets fetched!");

                if (fetchResult.IsSuccess && createBranch && tfsRemote.Id != GitTfsConstants.DefaultRepositoryId)
                {
                    Trace.WriteLine("Try creating the local branch...");
                    var branchRef = tfsRemote.Id.ToLocalGitRef();
                    if (!_globals.Repository.HasRef(branchRef))
                    {
                        if (!_globals.Repository.CreateBranch(branchRef, tfsRemote.MaxCommitHash))
                            Trace.TraceWarning("warning: Fail to create local branch ref file!");
                        else
                            Trace.WriteLine("Local branch created!");
                    }
                    else
                    {
                        Trace.TraceInformation("info: local branch ref already exists!");
                    }
                }
                return fetchResult;
            }
            finally
            {
                Trace.WriteLine("Cleaning...");
                tfsRemote.CleanupWorkspaceDirectory();
            }
        }
    }
}
