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
        public string ParentBranch { get; set; }
        public bool CloneAllBranches { get; set; }
        public string AuthorsFilePath { get; set; }
        public bool ShouldBeInteractive { get; set; }

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
                    { "interactive", "Run command in interactive mode", v => ShouldBeInteractive = (v != null) },
                    { "u|username=", "TFS username", v => TfsUsername = v },
                    { "p|password=", "TFS password", v => TfsPassword = v },
                    { "a|authors=", "Path to an Authors file to map TFS users to Git users", v => AuthorsFilePath = v },
                };
            }
        }

        private string GetBranchNames(string tfsRepositoryPath)
        {
            var expectedBranchName = ExtractGitBranchNameFromTfsRepositoryPath(tfsRepositoryPath);
            string expectedName;
            bool firstTime = true;
            do
            {
                if (!firstTime)
                    Console.WriteLine("Wrong branch name! Choose another one...");
                else
                    firstTime = false;
                Console.Write("Git remote name for tfs path " + tfsRepositoryPath + "(" + expectedBranchName + ")?");
                expectedName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(expectedName))
                {
                    return expectedBranchName;
                }
                expectedName = expectedName.Trim();
            } while (!_globals.Repository.IsValidBranchName(expectedName));

            return expectedName;
        }

        private Dictionary<string, string> GetBranchNames(IEnumerable<string> tfsRepositoryPathes)
        {
            var tmp = new Dictionary<string, string>(tfsRepositoryPathes.Count());
            foreach (var tfsRepositoryPath in tfsRepositoryPathes)
            {
                tmp.Add(tfsRepositoryPath, GetBranchNames(tfsRepositoryPath));
            }
            return tmp;
        }

        public int Run(string tfsBranchPath)
        {
            if (ShouldBeInteractive)
            {
                return Run(tfsBranchPath, GetBranchNames(tfsBranchPath));
            }
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
            if (!CloneAllBranches)
            {
                _helper.Run(this);
                return GitTfsExitCodes.Help;
            }

            Console.WriteLine("Loading datas...");

            var defaultRemote = InitFromDefaultRemote();

            var allRemotes = _globals.Repository.ReadAllTfsRemotes();

            var rootBranch = defaultRemote.Tfs.GetRootTfsBranchForRemotePath(defaultRemote.TfsRepositoryPath);
            if (rootBranch == null)
                throw new GitTfsException(string.Format("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '{0}' is not a TFS branch!", defaultRemote.TfsRepositoryPath));
            if (defaultRemote.TfsRepositoryPath.ToLower() != rootBranch.Path.ToLower())
               throw new GitTfsException(string.Format("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! Please clone again from '{0}'...", rootBranch.Path));

            var childBranchPaths = rootBranch.GetAllChildren().Select(b => b.Path);
            var childBranchPathsNotAlreadyFetched = childBranchPaths.Where(p => !allRemotes.Select(r => r.TfsRepositoryPath.ToLower()).Contains(p.ToLower())).ToList();

            if (!childBranchPathsNotAlreadyFetched.Any())
                throw new GitTfsException("error: no new tfs branches to init!");

            _stdout.WriteLine("New Tfs branches found:");
            foreach (var tfsBranchPath in childBranchPathsNotAlreadyFetched)
            {
                _stdout.WriteLine("- " + tfsBranchPath);
            }

            Dictionary<string, string> branchesWithExpectedNames = null;
            if (ShouldBeInteractive)
            {
                while (true)
                {
                    branchesWithExpectedNames = GetBranchNames(childBranchPathsNotAlreadyFetched);
                    //TODO : control branch name integrity!!! and already used
                    Console.WriteLine("Names that will be used for the tfs branches :");
                    foreach (var tfsBranchPath in branchesWithExpectedNames.Keys)
                    {
                        _stdout.WriteLine("- " + tfsBranchPath + " => " + branchesWithExpectedNames[tfsBranchPath]);
                    }
                    Console.Write("Are name ok?(Yes/No/Quit)");
                    var answer = (string.Empty + Console.ReadLine()).ToLower();
                    if (answer == "q") return GitTfsExitCodes.OK;
                    if (answer == "y") break;
                }
            }

            foreach (var tfsBranchPath in childBranchPathsNotAlreadyFetched)
            {
                int result;
                if (ShouldBeInteractive)
                    result = CreateBranch(defaultRemote, tfsBranchPath, allRemotes, branchesWithExpectedNames[tfsBranchPath]);
                else
                    result = CreateBranch(defaultRemote, tfsBranchPath, allRemotes);
                if (result < 0)
                    return result;
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
            _stdout.WriteLine("The name of the local branch will be : " + gitBranchName);
            if (string.IsNullOrWhiteSpace(gitBranchName))
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
            if (rootChangeSetId == -1)
                throw new GitTfsException("error: No root changeset found :( \n");
            Trace.WriteLine("Found root changeset : " + rootChangeSetId);

            Trace.WriteLine("Try to find changeset in git repository...");
            string sha1RootCommit = _globals.Repository.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetId + "[^0-9]");
            //sha1RootCommit = _globals.Repository.FindCommitByCommitMessage("git-tfs-id: .*\\$\\/" + tfsProject + "\\/.*;C" + rootChangeSetId + "[^0-9]");
            if (string.IsNullOrWhiteSpace(sha1RootCommit))
                throw new GitTfsException("error: The root changeset " + rootChangeSetId + " have not be found in the Git repository. The branch containing the changeset should not have been created. Please do it before retrying!!\n");
            Trace.WriteLine("Commit found! sha1 : " + sha1RootCommit);

            Trace.WriteLine("Try creating remote...");
            var tfsRemote = _globals.Repository.CreateTfsRemote(new RemoteInfo { Id = gitBranchName, Url = defaultRemote.TfsUrl, Repository = tfsRepositoryPath, RemoteOptions = _remoteOptions });
            if (!_globals.Repository.CreateBranch(tfsRemote.RemoteRef, sha1RootCommit))
                throw new GitTfsException("error: Fail to create remote branch ref file!");
            Trace.WriteLine("Remote created!");

            Trace.WriteLine("Try fetching changesets...");
            tfsRemote.Fetch();
            Trace.WriteLine("Changesets fetched!");

            Trace.WriteLine("Try creating the local branch...");
            if (!_globals.Repository.CreateBranch("refs/heads/" + gitBranchName, tfsRemote.MaxCommitHash))
                _stdout.WriteLine("warning: Fail to create local branch ref file!");
            else
                Trace.WriteLine("Local branch created!");

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
            return gitBranchName;
        }
    }
}
