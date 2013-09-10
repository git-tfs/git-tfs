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
    [Description("init-branch [$/Repository/path <git-branch-name-wished>|--all]\n ex : git tfs init-branch $/Repository/ProjectBranch\n      git tfs init-branch $/Repository/ProjectBranch myNewBranch\n      git tfs init-branch --all\n      git tfs init-branch --tfs-parent-branch=$/Repository/ProjectParentBranch $/Repository/ProjectBranch")]
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
            var defaultRemote = InitFromDefaultRemote();

            // TFS representations of repository paths do not have trailing slashes
            tfsBranchPath = (tfsBranchPath ?? string.Empty).TrimEnd('/');

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();

            tfsBranchPath.AssertValidTfsPath();
            return CreateBranch(defaultRemote, tfsBranchPath, allRemotes, gitBranchNameExpected, ParentBranch);
        }

        public int Run()
        {
            if (CloneAllBranches && NoFetch)
                throw new GitTfsException("error: --nofetch cannot be used with --all");

            if (!CloneAllBranches)
            {
                _helper.Run(this);
                return GitTfsExitCodes.Help;
            }

            var defaultRemote = InitFromDefaultRemote();

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();

            var rootBranch = defaultRemote.Tfs.GetRootTfsBranchForRemotePath(defaultRemote.TfsRepositoryPath);
            if (rootBranch == null)
                throw new GitTfsException(string.Format("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '{0}' is not a TFS branch!", defaultRemote.TfsRepositoryPath));
            if (defaultRemote.TfsRepositoryPath.ToLower() != rootBranch.Path.ToLower())
               throw new GitTfsException(string.Format("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! Please clone again from '{0}'...", rootBranch.Path));

            var childBranchPaths = rootBranch.GetAllChildren().Select(b=>b.Path).ToList();

            if (childBranchPaths.Any())
            {
                _stdout.WriteLine("Tfs branches found:");
                foreach (var tfsBranchPath in childBranchPaths)
                {
                    _stdout.WriteLine("- " + tfsBranchPath);
                }

                foreach (var tfsBranchPath in childBranchPaths)
                {
                    var result = CreateBranch(defaultRemote, tfsBranchPath, allRemotes);
                    if (result < 0)
                        return result;
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

        public int CreateBranch(IGitTfsRemote defaultRemote, string tfsRepositoryPath, IEnumerable<IGitTfsRemote> allRemotes, string gitBranchNameExpected = null, string tfsRepositoryPathParentBranch = null)
        {
            Trace.WriteLine("=> Working on TFS branch : " + tfsRepositoryPath);

            // TFS string representations of repository paths do not end in trailing slashes
            tfsRepositoryPath = (tfsRepositoryPath ?? string.Empty).TrimEnd('/');

            if (allRemotes.Count(r => r.TfsRepositoryPath.ToLower() == tfsRepositoryPath.ToLower()) != 0)
            {
                Trace.WriteLine("There is already a remote for this tfs branch. Branch ignored!");
                return GitTfsExitCodes.InvalidArguments;
            }

            string gitBranchName;
            if (!string.IsNullOrWhiteSpace(gitBranchNameExpected))
                gitBranchName = ExtractGitBranchNameFromTfsRepositoryPath(gitBranchNameExpected);
            else
                gitBranchName = ExtractGitBranchNameFromTfsRepositoryPath(tfsRepositoryPath);
            if(string.IsNullOrWhiteSpace(gitBranchName))
                throw new GitTfsException("error: The Git branch name '" + gitBranchName + "' is not valid...\n");
            Trace.WriteLine("Git local branch will be :" + gitBranchName);

            int rootChangeSetId;
            if (tfsRepositoryPathParentBranch == null)
                rootChangeSetId = defaultRemote.Tfs.GetRootChangesetForBranch(tfsRepositoryPath);
            else
            {
                var tfsRepositoryPathParentBranchFound = allRemotes.FirstOrDefault(r => r.TfsRepositoryPath.ToLower() == tfsRepositoryPathParentBranch.ToLower());
                if(tfsRepositoryPathParentBranchFound == null)
                    throw new GitTfsException("error: The Tfs parent branch '" + tfsRepositoryPathParentBranch + "' can not be found in the Git repository\nPlease init it first and try again...\n");

                rootChangeSetId = defaultRemote.Tfs.GetRootChangesetForBranch(tfsRepositoryPath, tfsRepositoryPathParentBranchFound.TfsRepositoryPath);
            }
            Trace.WriteLine("Found root changeset : " + rootChangeSetId);

            Trace.WriteLine("Try to find changeset in git repository...");
            string sha1RootCommit = _globals.Repository.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetId + "[^0-9]");
            //sha1RootCommit = _globals.Repository.FindCommitByCommitMessage("git-tfs-id: .*\\$\\/" + tfsProject + "\\/.*;C" + rootChangeSetId + "[^0-9]");
            if (string.IsNullOrWhiteSpace(sha1RootCommit))
                throw new GitTfsException("error: The root changeset " + rootChangeSetId + " have not be found in the Git repository. The branch containing the changeset should not have been created. Please do it before retrying!!\n");
            Trace.WriteLine("Commit found! sha1 : " + sha1RootCommit);

            Trace.WriteLine("Try creating remote...");
            var tfsRemote = _globals.Repository.CreateTfsRemote(new RemoteInfo { Id = gitBranchName, Url = defaultRemote.TfsUrl, Repository = tfsRepositoryPath, RemoteOptions = _remoteOptions }, System.String.Empty);
            if (!_globals.Repository.CreateBranch(tfsRemote.RemoteRef, sha1RootCommit))
                throw new GitTfsException("error: Fail to create remote branch ref file!");
            Trace.WriteLine("Remote created!");

            Trace.WriteLine("Try fetching changesets...");
            tfsRemote.Fetch();
            Trace.WriteLine("Changesets fetched!");

            if (!NoFetch)
            {
                Trace.WriteLine("Try fetching changesets...");
                tfsRemote.Fetch();
                Trace.WriteLine("Changesets fetched!");
            }
            else
            {
                Trace.WriteLine("Not fetching changesets, --nofetch option specified");
            }
            
            
            Trace.WriteLine("Try creating the local branch...");
            if (!_globals.Repository.CreateBranch(GitRepository.ShortToLocalName(gitBranchName), tfsRemote.MaxCommitHash))
                _stdout.WriteLine("warning: Fail to create local branch ref file!");
            else
                Trace.WriteLine("Local branch created!");

            Trace.WriteLine("Cleaning...");
            tfsRemote.CleanupWorkspaceDirectory();

            return GitTfsExitCodes.OK;
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
