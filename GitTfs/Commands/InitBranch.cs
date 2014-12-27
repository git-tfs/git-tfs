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
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Commands
{
    public class InitBranch : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly Globals _globals;
        private readonly Help _helper;
        private readonly AuthorsFile _authors;

        private RemoteOptions _remoteOptions;
        public string TfsUsername { get; set; }
        public string TfsPassword { get; set; }
        public string IgnoreRegex { get; set; }
        public string ExceptRegex { get; set; }
        public string ParentBranch { get; set; }
        public bool CloneAllBranches { get; set; }
        public bool NoFetch { get; set; }
        public bool DontCreateGitBranch { get; set; }

        public IGitTfsRemote RemoteCreated { get; private set; }

        public InitBranch(TextWriter stdout, Globals globals, Help helper, AuthorsFile authors)
        {
            _stdout = stdout;
            _globals = globals;
            _helper = helper;
            _authors = authors;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "all", "Clone all the TFS branches (For TFS 2010 and later)", v => CloneAllBranches = (v.ToLower() == "all") },
                    { "b|tfs-parent-branch=", "TFS Parent branch of the TFS branch to clone (TFS 2008 only! And required!!) ex: $/Repository/ProjectParentBranch", v => ParentBranch = v },
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
                _stdout.WriteLine("warning : There is already a remote for this tfs branch. Branch ignored!");
                return GitTfsExitCodes.InvalidArguments;
            }

            IList<RootBranch> creationBranchData;
            if (ParentBranch == null)
                creationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath);
            else
            {
                var tfsRepositoryPathParentBranchFound = allRemotes.FirstOrDefault(r => r.TfsRepositoryPath.ToLower() == ParentBranch.ToLower());
                if (tfsRepositoryPathParentBranchFound == null)
                    throw new GitTfsException("error: The Tfs parent branch '" + ParentBranch + "' can not be found in the Git repository\nPlease init it first and try again...\n");

                creationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath, -1, tfsRepositoryPathParentBranchFound.TfsRepositoryPath);
            }

            IFetchResult fetchResult;
            InitBranchSupportingRename(tfsBranchPath, gitBranchNameExpected, creationBranchData, defaultRemote, out fetchResult);
            return GitTfsExitCodes.OK;
        }

        private IGitTfsRemote InitBranchSupportingRename(string tfsBranchPath, string gitBranchNameExpected, IList<RootBranch> creationBranchData, IGitTfsRemote defaultRemote, out IFetchResult fetchResult)
        {
            fetchResult = null;

            RemoveAlreadyFetchedBranches(creationBranchData, defaultRemote);

            _stdout.WriteLine("Branches to Initialize successively :");
            foreach (var branch in creationBranchData)
                _stdout.WriteLine("-" + branch.TfsBranchPath + " (" + branch.RootChangeset + ")");

            IGitTfsRemote tfsRemote = null;
            var remoteToDelete = new List<IGitTfsRemote>();
            var renameContext = new RenameContext();
            foreach (var rootBranch in creationBranchData)
            {
                Trace.WriteLine("Processing " + (rootBranch.IsRenamedBranch ? "renamed " : string.Empty) + "branch :"
                    + rootBranch.TfsBranchPath + " (" + rootBranch.RootChangeset + ")");
                var cbd = new BranchCreationDatas() {RootChangesetId = rootBranch.RootChangeset, TfsRepositoryPath = rootBranch.TfsBranchPath};
                if (cbd.TfsRepositoryPath == tfsBranchPath)
                    cbd.GitBranchNameExpected = gitBranchNameExpected;

                tfsRemote = defaultRemote.InitBranch(_remoteOptions, cbd.TfsRepositoryPath, cbd.RootChangesetId, !NoFetch, cbd.GitBranchNameExpected, renameContext);
                if (tfsRemote == null)
                {
                    throw new GitTfsException("error: Couldn't fetch parent branch\n");
                }
                if (rootBranch.IsRenamedBranch || !NoFetch)
                {
                    fetchResult = FetchRemote(tfsRemote, false, !DontCreateGitBranch && !rootBranch.IsRenamedBranch, renameContext);
                    if(fetchResult.IsSuccess && rootBranch.IsRenamedBranch)
                        remoteToDelete.Add(tfsRemote);
                }
                else
                    Trace.WriteLine("Not fetching changesets, --no-fetch option specified");
            }
            foreach (var gitTfsRemote in remoteToDelete)
            {
                _globals.Repository.DeleteTfsRemote(gitTfsRemote);
            }
            return RemoteCreated = tfsRemote;
        }

        private static void RemoveAlreadyFetchedBranches(IList<RootBranch> creationBranchData, IGitTfsRemote defaultRemote)
        {
            for (int i = creationBranchData.Count - 1; i > 0; i--)
            {
                var branch = creationBranchData[i];
                if (defaultRemote.Repository.FindCommitHashByChangesetId(branch.RootChangeset) != null)
                {
                    for (int j = 0; j < i; j++)
                    {
                        creationBranchData.RemoveAt(0);
                    }
                    break;
                }
            }
        }

        class BranchCreationDatas
        {
            public string TfsRepositoryPath { get; set; }
            public string GitBranchNameExpected { get; set; }
            public long RootChangesetId { get; set; }
        }

        [DebuggerDisplay("{TfsRepositoryPath} C{RootChangesetId}")]
        class BranchDatas
        {
            public string TfsRepositoryPath { get; set; }
            public IGitTfsRemote TfsRemote { get; set; }
            public bool IsEntirelyFetched { get; set; }
            public long RootChangesetId { get; set; }
            public IList<RootBranch> CreationBranchData { get; set; }
            public Exception Error { get; set; }
        }

        private int CloneAll(string gitRemote)
        {
            if (CloneAllBranches && NoFetch)
                throw new GitTfsException("error: --no-fetch cannot be used with --all");

            _globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, false.ToString());

            var defaultRemote = InitFromDefaultRemote();

            if (gitRemote == null)
            {
                var childBranchPaths = GetChildBranchesToInit(defaultRemote);

                if (childBranchPaths.Any())
                {
                    InitializeBranches(defaultRemote, childBranchPaths);
                }
                else
                {
                    _stdout.WriteLine("No other Tfs branches found.");
                }
            }
            else
            {
                var branches = defaultRemote.Repository.GetGitRemoteBranches(gitRemote);
                var childBranchPaths = new Dictionary<string, BranchDatas>();
                foreach (var branch in branches)
                {
                    var remotes = _globals.Repository.GetLastParentTfsCommits(branch);
                    if (!remotes.Any())
                    {
                        continue;
                    }
                    var tfsPath = remotes.First().Remote.TfsRepositoryPath;
                    if (!childBranchPaths.ContainsKey(tfsPath))
                        childBranchPaths.Add(tfsPath, new BranchDatas { TfsRepositoryPath = tfsPath });
                }

                if (childBranchPaths.Any())
                {
                    InitializeBranches(defaultRemote, childBranchPaths.Values.ToList());
                }
                else
                {
                    _stdout.WriteLine("No other Tfs branches found.");
                }
            }
            return GitTfsExitCodes.OK;
        }

        private static List<BranchDatas> GetChildBranchesToInit(IGitTfsRemote defaultRemote)
        {
            var rootBranch = defaultRemote.Tfs.GetRootTfsBranchForRemotePath(defaultRemote.TfsRepositoryPath);
            if (rootBranch == null)
                throw new GitTfsException("error: The use of the option '--with-branches' to init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '"
                    + defaultRemote.TfsRepositoryPath + "' is not a TFS branch!");

            return rootBranch.GetAllChildrenOfBranch(defaultRemote.TfsRepositoryPath).Select(b => new BranchDatas { TfsRepositoryPath = b.Path }).ToList();
        }

        private void InitializeBranches(IGitTfsRemote defaultRemote, List<BranchDatas> childBranchPaths)
        {
            _stdout.WriteLine("Tfs branches found:");
            var branchesToProcess = new List<BranchDatas>();
            foreach (var tfsBranchPath in childBranchPaths)
            {
                _stdout.WriteLine("- " + tfsBranchPath.TfsRepositoryPath);
                var branchDatas = new BranchDatas
                    {
                        TfsRepositoryPath = tfsBranchPath.TfsRepositoryPath,
                        TfsRemote = _globals.Repository.ReadAllTfsRemotes().FirstOrDefault(r => r.TfsRepositoryPath == tfsBranchPath.TfsRepositoryPath)
                    };
                try
                {
                    branchDatas.CreationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath.TfsRepositoryPath);
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
                    _stdout.WriteLine("=> Working on TFS branch : " + tfsBranch.TfsRepositoryPath);
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
                            _stdout.WriteLine("error: an error occurs when initializing the branch. Branch is ignored and continuing...");
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
                            _stdout.WriteLine("error: an error occurs when fetching changeset. Fetching is stopped and continuing...");
                            tfsBranch.Error = ex;
                        }
                    }
                }
            } while (branchesToProcess.Any(b => !b.IsEntirelyFetched && b.Error == null) && isSomethingDone);

            _globals.Repository.GarbageCollect();

            if (branchesToProcess.Any(b => !b.IsEntirelyFetched))
            {
                _stdout.WriteLine("warning: Some Tfs branches could not have been initialized:");
                foreach (var branchNotInited in branchesToProcess.Where(b => !b.IsEntirelyFetched))
                {
                    _stdout.WriteLine("- " + branchNotInited.TfsRepositoryPath);
                }
                _stdout.WriteLine("\nPlease report this case to the git-tfs developers! (report here : https://github.com/git-tfs/git-tfs/issues/461 )");
            }
            if (branchesToProcess.Any(b => b.Error != null))
            {
                _stdout.WriteLine("warning: Some Tfs branches could not have been initialized or entirely fetched due to errors:");
                foreach (var branchWithErrors in branchesToProcess.Where(b => b.Error != null))
                {
                    _stdout.WriteLine("- " + branchWithErrors.TfsRepositoryPath);
                    if (_globals.DebugOutput)
                        Trace.WriteLine("   =>error:" + branchWithErrors.Error);
                    else
                        _stdout.WriteLine("   =>error:" + branchWithErrors.Error.Message);
                }
                _stdout.WriteLine("\nPlease report this case to the git-tfs developers! (report here : https://github.com/git-tfs/git-tfs/issues )");
            }
        }

        private IGitTfsRemote InitFromDefaultRemote()
        {
            var defaultRemote = _globals.Repository.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);
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


        private IFetchResult FetchRemote(IGitTfsRemote tfsRemote, bool stopOnFailMergeCommit, bool createBranch = true, RenameContext renameContext = null)
        {
            try
            {
                Trace.WriteLine("Try fetching changesets...");
                var fetchResult = tfsRemote.Fetch(stopOnFailMergeCommit, renameContext);
                Trace.WriteLine("Changesets fetched!");

                if (fetchResult.IsSuccess && createBranch && tfsRemote.Id != GitTfsConstants.DefaultRepositoryId)
                {
                    Trace.WriteLine("Try creating the local branch...");
                    var branchRef = tfsRemote.Id.ToLocalGitRef();
                    if (!_globals.Repository.HasRef(branchRef))
                    {
                        if (!_globals.Repository.CreateBranch(branchRef, tfsRemote.MaxCommitHash))
                            _stdout.WriteLine("warning: Fail to create local branch ref file!");
                        else
                            Trace.WriteLine("Local branch created!");
                    }
                    else
                    {
                        _stdout.WriteLine("info: local branch ref already exists!");
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
