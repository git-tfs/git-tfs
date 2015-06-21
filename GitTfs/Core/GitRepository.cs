using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StructureMap;
using LibGit2Sharp;
using Branch = LibGit2Sharp.Branch;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        private readonly IContainer _container;
        private readonly Globals _globals;
        private IDictionary<string, IGitTfsRemote> _cachedRemotes;
        private Repository _repository;
        private RemoteConfigConverter _remoteConfigReader;

        public GitRepository(TextWriter stdout, string gitDir, IContainer container, Globals globals, RemoteConfigConverter remoteConfigReader)
            : base(stdout, container)
        {
            _container = container;
            _globals = globals;
            GitDir = gitDir;
            _repository = new LibGit2Sharp.Repository(GitDir);
            _remoteConfigReader = remoteConfigReader;
        }

        ~GitRepository()
        {
            if (_repository != null)
                _repository.Dispose();
        }

        public GitCommit Commit(LogEntry logEntry)
        {
            var parents = logEntry.CommitParents.Select(sha => _repository.Lookup<Commit>(sha));
            var commit = _repository.ObjectDatabase.CreateCommit(
                new Signature(logEntry.AuthorName, logEntry.AuthorEmail, logEntry.Date.ToUniversalTime()),
                new Signature(logEntry.CommitterName, logEntry.CommitterEmail, logEntry.Date.ToUniversalTime()),
                logEntry.Log,
                logEntry.Tree,
                parents,
                false);
            changesetsCache[logEntry.ChangesetId] = commit.Sha;
            return new GitCommit(commit);
        }

        public void UpdateRef(string gitRefName, string shaCommit, string message = null)
        {
            if (message == null)
                _repository.Refs.Add(gitRefName, shaCommit, allowOverwrite: true);
            else
                _repository.Refs.Add(gitRefName, shaCommit, _repository.Config.BuildSignature(DateTime.Now), message, true);
        }

        public static string ShortToLocalName(string branchName)
        {
            return "refs/heads/" + branchName;
        }

        public string GitDir { get; set; }
        public string WorkingCopyPath { get; set; }
        public string WorkingCopySubdir { get; set; }

        protected override Process Start(string[] command, Action<ProcessStartInfo> initialize)
        {
            return base.Start(command, initialize.And(SetUpPaths));
        }

        private void SetUpPaths(ProcessStartInfo gitCommand)
        {
            if (GitDir != null)
                gitCommand.EnvironmentVariables["GIT_DIR"] = GitDir;
            if (WorkingCopyPath != null)
                gitCommand.WorkingDirectory = WorkingCopyPath;
            if (WorkingCopySubdir != null)
                gitCommand.WorkingDirectory = Path.Combine(gitCommand.WorkingDirectory, WorkingCopySubdir);
        }

        public string GetConfig(string key)
        {
            var entry = _repository.Config.Get<string>(key);
            return entry == null ? null : entry.Value;
        }

        public T GetConfig<T>(string key)
        {
            return GetConfig(key, default(T));
        }

        public T GetConfig<T>(string key, T defaultValue)
        {
            try
            {
                var entry = _repository.Config.Get<T>(key);
                if (entry == null)
                    return defaultValue;
                return entry.Value;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public void SetConfig(string key, string value)
        {
            _repository.Config.Set<string>(key, value, ConfigurationLevel.Local);
        }


        public IEnumerable<IGitTfsRemote> ReadAllTfsRemotes()
        {
            var remotes = GetTfsRemotes().Values;
            foreach (var remote in remotes)
                remote.EnsureTfsAuthenticated();

            return remotes;
        }

        public IGitTfsRemote ReadTfsRemote(string remoteId)
        {
            if (!HasRemote(remoteId))
                throw new GitTfsException("Unable to locate git-tfs remote with id = " + remoteId)
                    .WithRecommendation("Try using `git tfs bootstrap` to auto-init TFS remotes.");
            var remote = GetTfsRemotes()[remoteId];
            remote.EnsureTfsAuthenticated();
            return remote;
        }

        private IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            var allRemotes = GetTfsRemotes();
            var matchingRemotes =
                allRemotes.Values.Where(
                    remote => remote.MatchesUrlAndRepositoryPath(tfsUrl, tfsRepositoryPath));
            switch (matchingRemotes.Count())
            {
                case 0:
                    return new DerivedGitTfsRemote(tfsUrl, tfsRepositoryPath);
                case 1:
                    var remote = matchingRemotes.First();
                    return remote;
                default:
                    Trace.WriteLine("More than one remote matched!");
                    goto case 1;
            }
        }

        public IEnumerable<string> GetGitRemoteBranches(string gitRemote)
        {
            gitRemote = gitRemote + "/";
            var references = _repository.Branches.Where(b => b.IsRemote && b.Name.StartsWith(gitRemote) && !b.Name.EndsWith("/HEAD"));
            return references.Select(r => r.Name);
        }

        private IDictionary<string, IGitTfsRemote> GetTfsRemotes()
        {
            return _cachedRemotes ?? (_cachedRemotes = ReadTfsRemotes());
        }

        public IGitTfsRemote CreateTfsRemote(RemoteInfo remote, string autocrlf = null, string ignorecase = null)
        {
            if (HasRemote(remote.Id))
                throw new GitTfsException("A remote with id \"" + remote.Id + "\" already exists.");

            // The autocrlf default (as indicated by a null) is false and is set to override the system-wide setting.
            // When creating branches we use the empty string to indicate that we do not want to set the value at all.
            if (autocrlf == null)
                autocrlf = "false";
            if (autocrlf != String.Empty)
                _repository.Config.Set("core.autocrlf", autocrlf);

            if (ignorecase != null)
                _repository.Config.Set("core.ignorecase", ignorecase);

            foreach (var entry in _remoteConfigReader.Dump(remote))
            {
                if (entry.Value != null)
                {
                    _repository.Config.Set(entry.Key, entry.Value);
                }
                else
                {
                    _repository.Config.Unset(entry.Key);
                }
            }

            var gitTfsRemote = BuildRemote(remote);
            gitTfsRemote.EnsureTfsAuthenticated();

            return _cachedRemotes[remote.Id] = gitTfsRemote;
        }

        public void DeleteTfsRemote(IGitTfsRemote remote)
        {
            if (remote == null)
                throw new GitTfsException("error: the name of the remote to delete is invalid!");

            UnsetTfsRemoteConfig(remote.Id);
            _repository.Refs.Remove(remote.RemoteRef);
        }

        private void UnsetTfsRemoteConfig(string remoteId)
        {
            foreach (var entry in _remoteConfigReader.Delete(remoteId))
            {
                _repository.Config.Unset(entry.Key);
            }
            _cachedRemotes = null;
        }

        public void MoveRemote(string oldRemoteName, string newRemoteName)
        {
            if (!Reference.IsValidName(ShortToLocalName(oldRemoteName)))
                throw new GitTfsException("error: the name of the remote to move is invalid!");

            if (!Reference.IsValidName(ShortToLocalName(newRemoteName)))
                throw new GitTfsException("error: the new name of the remote is invalid!");

            if (HasRemote(newRemoteName))
                throw new GitTfsException(string.Format("error: this remote name \"{0}\" is already used!", newRemoteName));

            var oldRemote = ReadTfsRemote(oldRemoteName);
            if(oldRemote == null)
                throw new GitTfsException(string.Format("error: the remote \"{0}\" doesn't exist!", oldRemoteName));

            var remoteInfo = oldRemote.RemoteInfo;
            remoteInfo.Id = newRemoteName;

            CreateTfsRemote(remoteInfo);
            var newRemote = ReadTfsRemote(newRemoteName);

            _repository.Refs.Rename(oldRemote.RemoteRef, newRemote.RemoteRef);
            UnsetTfsRemoteConfig(oldRemoteName);
        }

        public Branch RenameBranch(string oldName, string newName)
        {
            var branch = _repository.Branches[oldName];

            if (branch == null)
                return null;

            return _repository.Branches.Rename(branch, newName);
        }

        private IDictionary<string, IGitTfsRemote> ReadTfsRemotes()
        {
            // does this need to ensuretfsauthenticated?
            _repository.Config.Set("tfs.touch", "1"); // reload configuration, because `git tfs init` and `git tfs clone` use Process.Start to update the config, so _repository's copy is out of date.
            return _remoteConfigReader.Load(_repository.Config).Select(x => BuildRemote(x)).ToDictionary(x => x.Id);
        }

        private IGitTfsRemote BuildRemote(RemoteInfo remoteInfo)
        {
            return _container.With(remoteInfo).With<IGitRepository>(this).GetInstance<IGitTfsRemote>();
        }

        public bool HasRemote(string remoteId)
        {
            return GetTfsRemotes().ContainsKey(remoteId);
        }

        public bool HasRef(string gitRef)
        {
            return _repository.Refs[gitRef] != null;
        }

        public void MoveTfsRefForwardIfNeeded(IGitTfsRemote remote)
        {
            long currentMaxChangesetId = remote.MaxChangesetId;
            var untrackedTfsChangesets = from cs in GetLastParentTfsCommits("HEAD")
                                         where cs.Remote.Id == remote.Id && cs.ChangesetId > currentMaxChangesetId
                                         orderby cs.ChangesetId
                                         select cs;
            foreach (var cs in untrackedTfsChangesets)
            {
                // UpdateTfsHead sets tag with TFS changeset id on each commit so we can't just update to latest
                remote.UpdateTfsHead(cs.GitCommit, cs.ChangesetId);
            }
        }

        public GitCommit GetCommit(string commitish)
        {
            return new GitCommit(_repository.Lookup<Commit>(commitish));
        }

        public MergeResult Merge(string commitish)
        {
            var commit = _repository.Lookup<Commit>(commitish);
            if(commit == null)
                throw new GitTfsException("error: commit '"+ commitish + "' can't be found and merged into!");
            return _repository.Merge(commit, _repository.Config.BuildSignature(new DateTimeOffset(DateTime.Now)));
        }

        public String GetCurrentCommit()
        {
            return _repository.Head.Commits.First().Sha;
        }

        public IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head)
        {
            var changesets = new List<TfsChangesetInfo>();
            var commit = _repository.Lookup<Commit>(head);
            if (commit == null)
                return changesets;
            FindTfsParentCommits(changesets, commit);
            return changesets;
        }

        private void FindTfsParentCommits(List<TfsChangesetInfo> changesets, Commit commit)
        {
            var commitsToFollow = new Stack<Commit>();
            commitsToFollow.Push(commit);
            var alreadyVisitedCommits = new HashSet<string>();
            while (commitsToFollow.Any())
            {
                commit = commitsToFollow.Pop();

                alreadyVisitedCommits.Add(commit.Sha);

                var changesetInfo = TryParseChangesetInfo(commit.Message, commit.Sha);
                if (changesetInfo == null)
                {
                    // If commit was not a TFS commit, continue searching all new parents of the commit
                    // Add parents in reverse order to keep topology (main parent should be treated first!)
                    foreach (var parent in commit.Parents.Where(x => !alreadyVisitedCommits.Contains(x.Sha)).Reverse())
                        commitsToFollow.Push(parent);
                }
                else
                {
                    changesets.Add(changesetInfo);
                }
            }
            Trace.WriteLine("Commits visited count:" + alreadyVisitedCommits.Count);
        }

        public TfsChangesetInfo GetTfsChangesetById(string remoteRef, long changesetId, string tfsPath)
        {
            var commit = FindCommitByChangesetId(changesetId, tfsPath, remoteRef);
            if (commit == null)
                return null;
            return TryParseChangesetInfo(commit.Message, commit.Sha);
        }

        public TfsChangesetInfo GetCurrentTfsCommit()
        {
            var currentCommit = _repository.Head.Commits.First();
            return TryParseChangesetInfo(currentCommit.Message, currentCommit.Sha);
        }

        public TfsChangesetInfo GetTfsCommit(GitCommit commit)
        {
            return TryParseChangesetInfo(commit.Message, commit.Sha);
        }

        public TfsChangesetInfo GetTfsCommit(string sha)
        {
            return GetTfsCommit(GetCommit(sha));
        }

        private TfsChangesetInfo TryParseChangesetInfo(string gitTfsMetaInfo, string commit)
        {
            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);
            if (match.Success)
            {
                var commitInfo = _container.GetInstance<TfsChangesetInfo>();
                commitInfo.Remote = ReadTfsRemote(match.Groups["url"].Value, match.Groups["repository"].Success ? match.Groups["repository"].Value : null);
                commitInfo.ChangesetId = Convert.ToInt32(match.Groups["changeset"].Value);
                commitInfo.GitCommit = commit;
                return commitInfo;
            }
            return null;
        }

        public IDictionary<string, GitObject> CreateObjectsDictionary()
        {
            return new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
        }

        public IDictionary<string, GitObject> GetObjects(string commit, IDictionary<string, GitObject> entries)
        {
            if (commit != null)
            {
                ParseEntries(entries, _repository.Lookup<Commit>(commit).Tree, commit);
            }
            return entries;
        }

        public IDictionary<string, GitObject> GetObjects(string commit)
        {
            var entries = CreateObjectsDictionary();
            return GetObjects(commit, entries);
        }

        public IGitTreeBuilder GetTreeBuilder(string commit)
        {
            if (commit == null)
            {
                return new GitTreeBuilder(_repository.ObjectDatabase);
            }
            else
            {
                return new GitTreeBuilder(_repository.ObjectDatabase, _repository.Lookup<Commit>(commit).Tree);
            }
        }

        public string GetCommitMessage(string head, string parentCommitish)
        {
            var message = new System.Text.StringBuilder();
            foreach (Commit comm in
                _repository.Commits.QueryBy(new CommitFilter { Since = head, Until = parentCommitish }))
            {
                // Normalize commit message line endings to CR+LF style, so that message
                // would be correctly shown in TFS commit dialog.
                message.AppendLine(NormalizeLineEndings(comm.Message));
            }

            return GitTfsConstants.TfsCommitInfoRegex.Replace(message.ToString(), "").Trim(' ', '\r', '\n');
        }

        private static string NormalizeLineEndings(string input)
        {
            return string.IsNullOrEmpty(input)
                ? input
                : input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        private void ParseEntries(IDictionary<string, GitObject> entries, Tree treeInfo, string commit)
        {
            var treesToDescend = new Queue<Tree>(new[] {treeInfo});
            while (treesToDescend.Any())
            {
                var currentTree = treesToDescend.Dequeue();
                foreach (var item in currentTree)
                {
                    if (item.TargetType == TreeEntryTargetType.Tree)
                    {
                        treesToDescend.Enqueue((Tree)item.Target);
                    }
                    var path = item.Path.Replace('\\', '/');
                    entries[path] = new GitObject
                    {
                        Mode = item.Mode,
                        Sha = item.Target.Sha,
                        ObjectType = item.TargetType,
                        Path = path,
                        Commit = commit
                    };
                }
            }
        }

        public IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to)
        {
            using (var diffOutput = CommandOutputPipe("diff-tree", "-r", "-M", "-z", from, to))
            {
                var changes = GitChangeInfo.GetChangedFiles(diffOutput);
                foreach (var change in changes)
                {
                    yield return BuildGitChangedFile(change);
                }
            }
        }

        private IGitChangedFile BuildGitChangedFile(GitChangeInfo change)
        {
            return change.ToGitChangedFile(_container.With((IGitRepository) this));
        }

        public bool WorkingCopyHasUnstagedOrUncommitedChanges
        {
            get
            {
                if (IsBare)
                    return false;
                return (from 
                            entry in _repository.RetrieveStatus()
                        where 
                            entry.State != FileStatus.Ignored &&
                            entry.State != FileStatus.Untracked
                        select entry).Any();
            }
        }

        public void CopyBlob(string sha, string outputFile)
        {
            Blob blob; 
            var destination = new FileInfo(outputFile);
            if (!destination.Directory.Exists)
                destination.Directory.Create();
            if ((blob = _repository.Lookup<Blob>(sha)) != null)
                using (Stream stream = blob.GetContentStream())
                using (var outstream = File.Create(destination.FullName))
                        stream.CopyTo(outstream);
        }

        public string AssertValidBranchName(string gitBranchName)
        {
            if (!Reference.IsValidName(ShortToLocalName(gitBranchName)))
                throw new GitTfsException("The name specified for the new git branch is not allowed. Choose another one!");
            return gitBranchName;
        }

        public bool CreateBranch(string gitBranchName, string target)
        {
            Reference reference;
            try
            {
                reference = _repository.Refs.Add(gitBranchName, target);
            }
            catch (Exception)
            {
                return false;
            }
            return reference != null;
        }

        private readonly Dictionary<long, string> changesetsCache = new Dictionary<long, string>();
        private bool cacheIsFull = false;

        public string FindCommitHashByChangesetId(long changesetId, string tfsPath)
        {
            var commit = FindCommitByChangesetId(changesetId, tfsPath);
            if (commit == null)
                return null;

            return commit.Sha;
        }

        private static readonly Regex tfsIdRegex = new Regex("^git-tfs-id: .*;C([0-9]+)\r?$", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static bool TryParseChangesetId(string commitMessage, out long changesetId)
        {
            var match = tfsIdRegex.Match(commitMessage);
            if (match.Success)
            {
                changesetId = long.Parse(match.Groups[1].Value);
                return true;
            }

            changesetId = 0;
            return false;
        }

        private Commit FindCommitByChangesetId(long changesetId, string tfsPath, string remoteRef = null)
        {
            Trace.WriteLine("Looking for changeset " + changesetId + " in git repository...");

            if (remoteRef == null)
            {
                string sha;
                if (changesetsCache.TryGetValue(changesetId, out sha))
                    return _repository.Lookup<Commit>(sha);
                if (cacheIsFull)
                    return null;
            }

            var reachableFromRemoteBranches = new CommitFilter
            {
                Since = _repository.Branches.Where(p => p.IsRemote),
                SortBy = CommitSortStrategies.Time
            };

            if (remoteRef != null)
                reachableFromRemoteBranches.Since = _repository.Branches.Where(p => p.IsRemote && p.CanonicalName.EndsWith(remoteRef));
            var commitsFromRemoteBranches = _repository.Commits.QueryBy(reachableFromRemoteBranches);

            Commit commit = null;
            foreach (var c in commitsFromRemoteBranches)
            {
                long id;
                if (TryParseChangesetId(c.Message, out id))
                {
                    changesetsCache[changesetId] = c.Sha;
                    if (id == changesetId && c.Message.Contains(tfsPath))
                    {
                        commit = c;
                        break;
                    }
                }
            }
            if (remoteRef == null && commit == null)
                cacheIsFull = true; // repository fully scanned
            Trace.WriteLine((commit == null) ? " => Commit not found!" : " => Commit found! hash: " + commit.Sha);
            return commit;
        }

        public void CreateTag(string name, string sha, string comment, string Owner, string emailOwner, System.DateTime creationDate)
        {
            if (_repository.Tags[name] == null)
                _repository.ApplyTag(name, sha, new Signature(Owner, emailOwner, new DateTimeOffset(creationDate)), comment);
        }

        public void CreateNote(string sha, string content, string owner, string emailOwner, DateTime creationDate)
        {
            Signature author = new Signature(owner, emailOwner, creationDate);
            _repository.Notes.Add(new ObjectId(sha), content, author, author, "commits");
        }

        public void ResetHard(string sha)
        {
            _repository.Reset(ResetMode.Hard, sha);
        }

        public bool IsBare { get { return _repository.Info.IsBare; } }

        /// <summary>
        /// Gets all configured "subtree" remotes which point to the same Tfs URL as the given remote.
        /// If the given remote is itself a subtree, an empty enumerable is returned.
        /// </summary>
        public IEnumerable<IGitTfsRemote> GetSubtrees(IGitTfsRemote owner)
        {
            //a subtree remote cannot have subtrees itself.
            if (owner.IsSubtree)
                return Enumerable.Empty<IGitTfsRemote>();

            return ReadAllTfsRemotes().Where(x => x.IsSubtree && string.Equals(x.OwningRemoteId, owner.Id, StringComparison.InvariantCultureIgnoreCase));
        }

        public void ResetRemote(IGitTfsRemote remoteToReset, string target)
        {
            _repository.Refs.UpdateTarget(remoteToReset.RemoteRef, target);
        }

        public string GetCurrentBranch()
        {
            return _repository.Head.CanonicalName;
        }

        public void GarbageCollect(bool auto, string additionalMessage)
        {
            try
            {
                if (auto)
                    _globals.Repository.CommandNoisy("gc", "--auto");
                else
                    _globals.Repository.CommandNoisy("gc");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                realStdout.WriteLine("Warning: `git gc` failed! " + additionalMessage);
            }
        }

        public bool Checkout(string commitish)
        {
            try
            {
                _repository.Checkout(commitish);
                return true;
            }
            catch (MergeConflictException ex)
            {
                return false;
            }
        }

        public IEnumerable<GitCommit> FindParentCommits(string @from, string to)
        {
            var commits = _repository.Commits.QueryBy(
                new CommitFilter() {Since = @from, Until = to, SortBy = CommitSortStrategies.Reverse, FirstParentOnly = true})
                .Select(c=>new GitCommit(c));
            var parent = to;
            foreach (var gitCommit in commits)
            {
                if(!gitCommit.Parents.Any(c=>c.Sha == parent))
                    return new List<GitCommit>();
                parent = gitCommit.Sha;
            }
            return commits;
        }
    }
}
