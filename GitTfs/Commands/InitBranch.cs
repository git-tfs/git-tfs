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

        public int Run(string tfsBranchPath)
        {
            return Run(tfsBranchPath, null);
        }

        public int Run(string tfsBranchPath, string gitBranchNameExpected)
        {
            var defaultRemote = InitFromDefaultRemote();

            // TFS representations of repository paths do not have trailing slashes
            tfsBranchPath = (tfsBranchPath ?? string.Empty).TrimEnd('/');

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();

            tfsBranchPath.AssertValidTfsPath();
            if (allRemotes.Any(r => r.TfsRepositoryPath.ToLower() == tfsBranchPath.ToLower()))
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

                creationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath, tfsRepositoryPathParentBranchFound.TfsRepositoryPath);
            }

            IFetchResult fetchResult;
            InitBranchSupportingRename(tfsBranchPath, gitBranchNameExpected, creationBranchData, defaultRemote, true, out fetchResult);
            return GitTfsExitCodes.OK;
        }

        private IGitTfsRemote InitBranchSupportingRename(string tfsBranchPath, string gitBranchNameExpected, IList<RootBranch> creationBranchData, IGitTfsRemote defaultRemote, bool failWithException, out IFetchResult fetchResult)
        {
            fetchResult = null;
            _stdout.WriteLine("Branches to Initialize successively :");
            foreach (var branch in creationBranchData)
                _stdout.WriteLine("-" + branch.TfsBranchPath + " (" + branch.RootChangeset + ")");

            IGitTfsRemote tfsRemote = null;
            var remoteToDelete = new List<IGitTfsRemote>();
            foreach (var rootBranch in creationBranchData)
            {
                Trace.WriteLine("Processing " + (rootBranch.IsRenamedBranch ? "renamed " : string.Empty) + "branch :" + rootBranch.TfsBranchPath + " (" + rootBranch.RootChangeset + ")");
                var cbd = new BranchCreationDatas() {RootChangesetId = rootBranch.RootChangeset, TfsRepositoryPath = rootBranch.TfsBranchPath};
                if (cbd.TfsRepositoryPath == tfsBranchPath)
                    cbd.GitBranchNameExpected = gitBranchNameExpected;

                cbd.Sha1RootCommit = _globals.Repository.FindCommitHashByChangesetId(cbd.RootChangesetId);
                if (string.IsNullOrWhiteSpace(cbd.Sha1RootCommit))
                {
                    if (failWithException)
                        throw new GitTfsException("error: The root changeset " + cbd.RootChangesetId +
                                              " have not be found in the Git repository. The branch containing the changeset should not have been created. Please do it before retrying!!\n");
                    return null;
                }

                Trace.WriteLine("Found commit " + cbd.Sha1RootCommit + " for changeset :" + cbd.RootChangesetId);

                tfsRemote = CreateBranch(defaultRemote, cbd.TfsRepositoryPath, cbd.Sha1RootCommit, cbd.GitBranchNameExpected);
                RemoteCreated = tfsRemote;
                if (rootBranch.IsRenamedBranch || !NoFetch)
                {
                    fetchResult = FetchRemote(tfsRemote, false, !DontCreateGitBranch && !rootBranch.IsRenamedBranch);
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
            return tfsRemote;
        }

        class BranchCreationDatas
        {
            public string TfsRepositoryPath { get; set; }
            public string GitBranchNameExpected { get; set; }
            public long RootChangesetId { get; set; }
            public string Sha1RootCommit { get; set; }
        }

        [DebuggerDisplay("{TfsRepositoryPath} C{RootChangesetId}")]
        class BranchDatas
        {
            public string TfsRepositoryPath { get; set; }
            public IGitTfsRemote TfsRemote { get; set; }
            public bool IsEntirelyFetched { get; set; }
            public long RootChangesetId { get; set; }
            public IList<RootBranch> CreationBranchData { get; set; }
        }

        public int Run()
        {
            if (CloneAllBranches && NoFetch)
                throw new GitTfsException("error: --no-fetch cannot be used with --all");

            if (!CloneAllBranches)
            {
                _helper.Run(this);
                return GitTfsExitCodes.Help;
            }

            var defaultRemote = InitFromDefaultRemote();

            var rootBranch = defaultRemote.Tfs.GetRootTfsBranchForRemotePath(defaultRemote.TfsRepositoryPath);
            if (rootBranch == null)
                throw new GitTfsException(string.Format("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '{0}' is not a TFS branch!", defaultRemote.TfsRepositoryPath));
            if (defaultRemote.TfsRepositoryPath.ToLower() != rootBranch.Path.ToLower())
               throw new GitTfsException(string.Format("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! Please clone again from '{0}'...", rootBranch.Path));

            var childBranchPaths = rootBranch.GetAllChildren().Select(b => new BranchDatas {TfsRepositoryPath = b.Path}).ToList();

            if (childBranchPaths.Any())
            {
                _stdout.WriteLine("Tfs branches found:");
                var branchesToProcess = new List<BranchDatas>();
                foreach (var tfsBranchPath in childBranchPaths)
                {
                    _stdout.WriteLine("- " + tfsBranchPath.TfsRepositoryPath);
                    var branchDatas = new BranchDatas
                        {
                            TfsRepositoryPath = tfsBranchPath.TfsRepositoryPath,
                            CreationBranchData = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath.TfsRepositoryPath)
                        };
                    branchesToProcess.Add(branchDatas);
                }
                branchesToProcess.Add(new BranchDatas {TfsRepositoryPath = defaultRemote.TfsRepositoryPath, TfsRemote = defaultRemote, RootChangesetId = -1});

                bool isSomethingDone;
                do
                {
                    isSomethingDone = false;
                    var branchesToFetch = branchesToProcess.Where(b => !b.IsEntirelyFetched).ToList();
                    foreach (var tfsBranch in branchesToFetch)
                    {
                        Trace.WriteLine("=> Working on TFS branch : " + tfsBranch.TfsRepositoryPath);
                        if (tfsBranch.TfsRemote == null)
                        {
                            IFetchResult fetchResult;
                            tfsBranch.TfsRemote = InitBranchSupportingRename(tfsBranch.TfsRepositoryPath, null, tfsBranch.CreationBranchData, defaultRemote, false, out fetchResult);
                            if (tfsBranch.TfsRemote != null)
                            {
                                tfsBranch.IsEntirelyFetched = fetchResult.IsSuccess;
                                isSomethingDone = true;
                            }
                        }
                        else
                        {
                            var lastFetchedChangesetId = tfsBranch.TfsRemote.MaxChangesetId;
                            Trace.WriteLine("Fetching remote :" + tfsBranch.TfsRemote.Id);
                            var fetchResult = FetchRemote(tfsBranch.TfsRemote, true);
                            tfsBranch.IsEntirelyFetched = fetchResult.IsSuccess;
                            if (lastFetchedChangesetId != fetchResult.LastFetchedChangesetId)
                                isSomethingDone = true;
                        }
                    }
                } while (branchesToProcess.Any(b => !b.IsEntirelyFetched) && isSomethingDone);

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
            }
            else
            {
                _stdout.WriteLine("No other Tfs branches found.");
            }
            return GitTfsExitCodes.OK;
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

        private IGitTfsRemote CreateBranch(IGitTfsRemote defaultRemote, string tfsRepositoryPath, string sha1RootCommit, string gitBranchNameExpected = null)
        {
            Trace.WriteLine("Begin process of creating branch for remote :" + tfsRepositoryPath);
            // TFS string representations of repository paths do not end in trailing slashes
            tfsRepositoryPath = (tfsRepositoryPath ?? string.Empty).TrimEnd('/');

            string gitBranchName;
            if (!string.IsNullOrWhiteSpace(gitBranchNameExpected))
                gitBranchName = ExtractGitBranchNameFromTfsRepositoryPath(gitBranchNameExpected);
            else
                gitBranchName = ExtractGitBranchNameFromTfsRepositoryPath(tfsRepositoryPath);
            if (string.IsNullOrWhiteSpace(gitBranchName))
                throw new GitTfsException("error: The Git branch name '" + gitBranchName + "' is not valid...\n");
            Trace.WriteLine("Git local branch will be :" + gitBranchName);

            if (_globals.Repository.HasRemote(gitBranchName))
            {
                Trace.WriteLine("Remote already exist");
                var remote = _globals.Repository.ReadTfsRemote(gitBranchName);
                if (remote.TfsUrl != defaultRemote.TfsUrl)
                    Trace.WriteLine("warning: Url is different");
                if (remote.TfsRepositoryPath != tfsRepositoryPath)
                    Trace.WriteLine("warning: TFS repository path is different");
                return remote;
            }

            Trace.WriteLine("Try creating remote...");
            var tfsRemote = _globals.Repository.CreateTfsRemote(new RemoteInfo
                {
                    Id = gitBranchName,
                    Url = defaultRemote.TfsUrl,
                    Repository = tfsRepositoryPath,
                    RemoteOptions = _remoteOptions
                }, String.Empty);

            if (!_globals.Repository.CreateBranch(tfsRemote.RemoteRef, sha1RootCommit))
                throw new GitTfsException("error: Fail to create remote branch ref file!");

            Trace.WriteLine("Remote created!");
            return tfsRemote;
        }

        private IFetchResult FetchRemote(IGitTfsRemote tfsRemote, bool stopOnFailMergeCommit, bool createBranch = true)
        {
            try
            {
                Trace.WriteLine("Try fetching changesets...");
                var fetchResult = tfsRemote.Fetch(stopOnFailMergeCommit);
                Trace.WriteLine("Changesets fetched!");

                if (fetchResult.IsSuccess && createBranch && tfsRemote.Id != GitTfsConstants.DefaultRepositoryId)
                {
                    Trace.WriteLine("Try creating the local branch...");
                    if (!_globals.Repository.CreateBranch("refs/heads/" + tfsRemote.Id, tfsRemote.MaxCommitHash))
                        _stdout.WriteLine("warning: Fail to create local branch ref file!");
                    else
                        Trace.WriteLine("Local branch created!");
                }
                return fetchResult;
            }
            finally
            {
                Trace.WriteLine("Cleaning...");
                tfsRemote.CleanupWorkspaceDirectory();
            }
        }

        protected string ExtractGitBranchNameFromTfsRepositoryPath(string tfsRepositoryPath)
        {
            string gitBranchNameExpected;
            if (tfsRepositoryPath.IndexOf("$/") == 0)
            {
                gitBranchNameExpected = tfsRepositoryPath.Remove(0, tfsRepositoryPath.IndexOf('/', 2) + 1);
            }
            else
            {
                gitBranchNameExpected = tfsRepositoryPath;
            }
            gitBranchNameExpected = gitBranchNameExpected.ToGitRefName();
            var gitBranchName = _globals.Repository.AssertValidBranchName(gitBranchNameExpected);
            _stdout.WriteLine("The name of the local branch will be : " + gitBranchName);
            return gitBranchName;
        }
    }
}
