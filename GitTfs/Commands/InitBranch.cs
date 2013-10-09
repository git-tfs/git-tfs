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
    [Pluggable("init-branch")]
    [Description("WARNING: This command is obsolete and will be removed in the next version. Use 'branch --init' instead!\n\ninit-branch [$/Repository/path <git-branch-name-wished>|--all]\n ex : git tfs init-branch $/Repository/ProjectBranch\n      git tfs init-branch $/Repository/ProjectBranch myNewBranch\n      git tfs init-branch --all\n      git tfs init-branch --tfs-parent-branch=$/Repository/ProjectParentBranch $/Repository/ProjectBranch")]
    [RequiresValidGitRepository]
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
        public string AuthorsFilePath { get; set; }
        public bool NoFetch { get; set; }

        //[Temporary] Remove in the next version!
        public bool DontDisplayObsoleteMessage { get; set; }

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
                    { "a|authors=", "Path to an Authors file to map TFS users to Git users", v => AuthorsFilePath = v },
                    { "ignore-regex=", "a regex of files to ignore", v => IgnoreRegex = v },
                    { "except-regex=", "a regex of exceptions to ignore-regex", v => ExceptRegex = v},
                    { "nofetch", "Create the new TFS remote but don't fetch any changesets", v => NoFetch = (v != null) }
                };
            }
        }

        public int Run(string tfsBranchPath)
        {
            return Run(tfsBranchPath, null);
        }

        public int Run(string tfsBranchPath, string gitBranchNameExpected)
        {
            //[Temporary] Remove in the next version!
            if (!DontDisplayObsoleteMessage)
                _stdout.WriteLine("WARNING: This command is obsolete and will be removed in the next version. Use 'branch --init' instead!");

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

            int rootChangeSetId;
            if (ParentBranch == null)
                rootChangeSetId = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath);
            else
            {
                var tfsRepositoryPathParentBranchFound = allRemotes.FirstOrDefault(r => r.TfsRepositoryPath.ToLower() == ParentBranch.ToLower());
                if (tfsRepositoryPathParentBranchFound == null)
                    throw new GitTfsException("error: The Tfs parent branch '" + ParentBranch + "' can not be found in the Git repository\nPlease init it first and try again...\n");

                rootChangeSetId = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath, tfsRepositoryPathParentBranchFound.TfsRepositoryPath);
            }

            var sha1RootCommit = FindChangesetInGitRepository(rootChangeSetId);
            if (string.IsNullOrWhiteSpace(sha1RootCommit))
                throw new GitTfsException("error: The root changeset " + rootChangeSetId +
                                          " have not be found in the Git repository. The branch containing the changeset should not have been created. Please do it before retrying!!\n");
            var tfsRemote = CreateBranch(defaultRemote, tfsBranchPath, sha1RootCommit, gitBranchNameExpected);
            if (!NoFetch)
                FetchRemote(tfsRemote, false);
            else
                Trace.WriteLine("Not fetching changesets, --nofetch option specified");
            return GitTfsExitCodes.OK;
        }

        class BranchDatas
        {
            public string TfsRepositoryPath { get; set; }
            public IGitTfsRemote TfsRemote { get; set; }
            public bool IsEntirelyFetched { get; set; }
            public long RootChangesetId { get; set; }
        }

        public int Run()
        {
            //[Temporary] Remove in the next version!
            if (!DontDisplayObsoleteMessage)
                _stdout.WriteLine("WARNING: This command is obsolete and will be removed in the next version. Use 'branch --init' instead!");

            if (CloneAllBranches && NoFetch)
                throw new GitTfsException("error: --nofetch cannot be used with --all");

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
                foreach (var tfsBranchPath in childBranchPaths)
                {
                    _stdout.WriteLine("- " + tfsBranchPath.TfsRepositoryPath);
                    tfsBranchPath.RootChangesetId = defaultRemote.Tfs.GetRootChangesetForBranch(tfsBranchPath.TfsRepositoryPath);
                }
                childBranchPaths.Add(new BranchDatas {TfsRepositoryPath = defaultRemote.TfsRepositoryPath, TfsRemote = defaultRemote, RootChangesetId = -1});

                bool isSomethingDone;
                do
                {
                    isSomethingDone = false;
                    var branchesToFetch = childBranchPaths.Where(b => !b.IsEntirelyFetched).ToList();
                    foreach (var tfsBranch in branchesToFetch)
                    {
                        Trace.WriteLine("=> Working on TFS branch : " + tfsBranch.TfsRepositoryPath);
                        if (tfsBranch.TfsRemote == null)
                        {
                            var sha1RootCommit = FindChangesetInGitRepository(tfsBranch.RootChangesetId);
                            if (sha1RootCommit != null)
                            {
                                tfsBranch.TfsRemote = CreateBranch(defaultRemote, tfsBranch.TfsRepositoryPath, sha1RootCommit);
                                isSomethingDone = true;
                            }
                        }
                        if (tfsBranch.TfsRemote != null)
                        {
                            var lastFetchedChangesetId = tfsBranch.TfsRemote.MaxChangesetId;
                            Trace.WriteLine("Fetching remote :" + tfsBranch.TfsRemote.Id);
                            var fetchResult = FetchRemote(tfsBranch.TfsRemote, true);
                            tfsBranch.IsEntirelyFetched = fetchResult.IsSuccess;
                            if (lastFetchedChangesetId != fetchResult.LastFetchedChangesetId)
                                isSomethingDone = true;
                        }
                    }
                } while (childBranchPaths.Any(b => !b.IsEntirelyFetched) && isSomethingDone);

                if (childBranchPaths.Any(b => !b.IsEntirelyFetched))
                {
                    _stdout.WriteLine("warning: Some Tfs branches could not have been initialized:");
                    foreach (var branchNotInited in childBranchPaths.Where(b => !b.IsEntirelyFetched))
                    {
                        _stdout.WriteLine("- " + branchNotInited.TfsRepositoryPath);
                    }
                    _stdout.WriteLine("\nPlease report this case to the git-tfs developpers! (report here : https://github.com/git-tfs/git-tfs/issues/461 )");
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

            _authors.Parse(AuthorsFilePath, _globals.GitDir);

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

            Trace.WriteLine("Try creating remote...");
            var tfsRemote = _globals.Repository.CreateTfsRemote(new RemoteInfo
                {
                    Id = gitBranchName,
                    Url = defaultRemote.TfsUrl,
                    Repository = tfsRepositoryPath,
                    RemoteOptions = _remoteOptions
                }, System.String.Empty);
            if (!_globals.Repository.CreateBranch(tfsRemote.RemoteRef, sha1RootCommit))
                throw new GitTfsException("error: Fail to create remote branch ref file!");
            Trace.WriteLine("Remote created!");
            return tfsRemote;
        }

        private string FindChangesetInGitRepository(long rootChangeSetId)
        {
            Trace.WriteLine("Found root changeset : " + rootChangeSetId);
            Trace.WriteLine("Try to find changeset " + rootChangeSetId + " in git repository...");
            string sha1RootCommit = _globals.Repository.FindCommitHashByChangesetId(rootChangeSetId);
            //sha1RootCommit = _globals.Repository.FindCommitByCommitMessage("git-tfs-id: .*\\$\\/" + tfsProject + "\\/.*;C" + rootChangeSetId + "[^0-9]");
            if (sha1RootCommit != null)
            Trace.WriteLine("Commit found! sha1 : " + sha1RootCommit);
            else
                Trace.WriteLine("Commit not found!");
            return sha1RootCommit;
        }

        private IFetchResult FetchRemote(IGitTfsRemote tfsRemote, bool stopOnFailMergeCommit)
        {
            Trace.WriteLine("Try fetching changesets...");
            var fetchResult = tfsRemote.Fetch(stopOnFailMergeCommit);
            Trace.WriteLine("Changesets fetched!");

            if (fetchResult.IsSuccess && tfsRemote.Id != GitTfsConstants.DefaultRepositoryId)
            {
                Trace.WriteLine("Try creating the local branch...");
                if (!_globals.Repository.CreateBranch("refs/heads/" + tfsRemote.Id, tfsRemote.MaxCommitHash))
                    _stdout.WriteLine("warning: Fail to create local branch ref file!");
                else
                    Trace.WriteLine("Local branch created!");
            }

            Trace.WriteLine("Cleaning...");
            tfsRemote.CleanupWorkspaceDirectory();

            return fetchResult;
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
