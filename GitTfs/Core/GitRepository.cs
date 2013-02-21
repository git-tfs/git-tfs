using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;
using LibGit2Sharp;
using Branch = LibGit2Sharp.Branch;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        private readonly IContainer _container;
        private readonly Globals _globals;
        private static readonly Regex configLineRegex = new Regex("^tfs-remote\\.(?<id>.+)\\.(?<key>[^.=]+)=(?<value>.*)$");
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
            return _repository.Config.Get<string>(key, null);
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

        private IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath, bool includeStubRemotes)
        {
            var allRemotes = GetTfsRemotes();
            var matchingRemotes =
                allRemotes.Values.Where(
                    remote => remote.MatchesUrlAndRepositoryPath(tfsUrl, tfsRepositoryPath));
            switch (matchingRemotes.Count())
            {
                case 0:
                    if (!includeStubRemotes)
                        throw new GitTfsException("Unable to locate a remote for <" + tfsUrl + ">" + tfsRepositoryPath)
                            .WithRecommendation("Try using `git tfs bootstrap` to auto-init TFS remotes.")
                            .WithRecommendation("Try setting a legacy-url for an existing remote.");
                    return new DerivedGitTfsRemote(tfsUrl, tfsRepositoryPath);
                case 1:
                    Trace.WriteLine("One remote matched");
                    var remote = matchingRemotes.First();
                    remote.EnsureTfsAuthenticated();
                    return remote;
                default:
                    Trace.WriteLine("More than one remote matched!");
                    goto case 1;
            }
        }

        private IDictionary<string, IGitTfsRemote> GetTfsRemotes()
        {
            return _cachedRemotes ?? (_cachedRemotes = ReadTfsRemotes());
        }

        public IGitTfsRemote CreateTfsRemote(RemoteInfo remote)
        {
            if (HasRemote(remote.Id))
                throw new GitTfsException("A remote with id \"" + remote.Id + "\" already exists.");

            // These help the new (if it's new) git repository to behave more sanely.
            _repository.Config.Set("core.autocrlf", "false");
            _repository.Config.Set("core.ignorecase", "false");

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
            if (!_repository.Refs.IsValidName("refs/heads/" + oldRemoteName))
                throw new GitTfsException("error: the name of the remote to move is invalid!");

            if (!_repository.Refs.IsValidName("refs/heads/" + newRemoteName))
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

            _repository.Refs.Move(oldRemote.RemoteRef, newRemote.RemoteRef);
            UnsetTfsRemoteConfig(oldRemoteName);
        }

        public Branch RenameBranch(string oldName, string newName)
        {
            var branch = _repository.Branches[oldName];

            if (branch == null)
                return null;

            return _repository.Branches.Move(branch, newName);
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
            var untrackedTfsChangesets = from cs in GetParentTfsCommits("refs/remotes/tfs/" + remote.Id + "..HEAD", false)
                                         where cs.Remote.Id == remote.Id && cs.ChangesetId > currentMaxChangesetId
                                         orderby cs.ChangesetId
                                         select cs;
            foreach (var cs in untrackedTfsChangesets)
            {
                // UpdateRef sets tag with TFS changeset id on each commit so we can't just update to latest
                remote.UpdateRef(cs.GitCommit, cs.ChangesetId);
            }
        }

        public GitCommit GetCommit(string commitish)
        {
            return new GitCommit(_repository.Lookup<Commit>(commitish));
        }

        public IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head)
        {
            return GetLastParentTfsCommits(head, false);
        }

        public IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head, bool includeStubRemotes)
        {
            List<TfsChangesetInfo> tfsCommits = GetParentTfsCommits(head, includeStubRemotes);
            return from commit in tfsCommits
                   group commit by commit.Remote
                   into remotes
                   select remotes.OrderBy(commit => -commit.ChangesetId).First();
        }

        private List<TfsChangesetInfo> GetParentTfsCommits(string head, bool includeStubRemotes)
        {
            var tfsCommits = new List<TfsChangesetInfo>();
            try
            {
                CommandOutputPipe(stdout => FindTfsCommits(stdout, tfsCommits, includeStubRemotes),
                                  "log", "--no-color", "--pretty=medium", head);
            }
            catch (GitCommandException e)
            {
                Trace.WriteLine("An error occurred while loading head " + head + " (maybe it doesn't exist?): " + e);
            }
            return tfsCommits;
        }

        public TfsChangesetInfo GetCurrentTfsCommit()
        {
            var currentCommit = _repository.Head.Commits.First();
            return TryParseChangesetInfo(currentCommit.Message, currentCommit.Sha, false);
        }

        private void FindTfsCommits(TextReader stdout, ICollection<TfsChangesetInfo> tfsCommits, bool includeStubRemotes)
        {
            string currentCommit = null;
            TfsChangesetInfo lastChangesetInfo = null;
            string line;
            while (null != (line = stdout.ReadLine()))
            {
                var match = GitTfsConstants.CommitRegex.Match(line);
                if (match.Success)
                {
                    if (lastChangesetInfo != null && currentCommit != null)
                    {
                        tfsCommits.Add(lastChangesetInfo);
                        currentCommit = null;
                        lastChangesetInfo = null;
                    }
                    currentCommit = match.Groups[1].Value;
                    continue;
                }
                var changesetInfo = TryParseChangesetInfo(line, currentCommit, includeStubRemotes);
                if (changesetInfo != null)
                {
                    lastChangesetInfo = changesetInfo;
                }
            }

            // Add the final changesetinfo object; it won't be handled in the loop
            // if it was part of the last commit message.
            if (lastChangesetInfo != null && currentCommit != null)
                tfsCommits.Add(lastChangesetInfo);

            //stdout.Close();
        }

        private TfsChangesetInfo TryParseChangesetInfo(string gitTfsMetaInfo, string commit, bool includeStubRemotes)
        {
            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);
            if (match.Success)
            {
                var commitInfo = _container.GetInstance<TfsChangesetInfo>();
                commitInfo.Remote = ReadTfsRemote(match.Groups["url"].Value, match.Groups["repository"].Value, includeStubRemotes);
                commitInfo.ChangesetId = Convert.ToInt32(match.Groups["changeset"].Value);
                commitInfo.GitCommit = commit;
                return commitInfo;
            }
            return null;
        }

        public IDictionary<string, GitObject> GetObjects(string commit)
        {
            var entries = GetObjects();
            if (commit != null)
            {
                ParseEntries(entries, _repository.Lookup<Commit>(commit).Tree, commit);
            }
            return entries;
        }

        public Dictionary<string, GitObject> GetObjects()
        {
            return new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string GetCommitMessage(string head, string parentCommitish)
        {
            var message = new System.Text.StringBuilder();
            foreach (LibGit2Sharp.Commit comm in
                _repository.Commits.QueryBy(new LibGit2Sharp.Filter { Since = head, Until = parentCommitish }))
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
                    if (item.Type == GitObjectType.Tree)
                    {
                        treesToDescend.Enqueue((Tree)item.Target);
                    }
                    var path = item.Path.Replace('\\', '/');
                    entries[path] = new GitObject
                    {
                        Mode = item.Mode.ToModeString(),
                        Sha = item.Target.Sha,
                        ObjectType = item.Type.ToString().ToLower(),
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
                return (from 
                            entry in _repository.Index.RetrieveStatus()
                        where 
                             entry.State != FileStatus.Ignored &&
                             entry.State != FileStatus.Untracked
                        select entry).Count() > 0;
            }
        }

        public void CopyBlob(string sha, string outputFile)
        {
            Blob blob; 
            var destination = new FileInfo(outputFile);
            if (!destination.Directory.Exists)
                destination.Directory.Create();
            if ((blob = _repository.Lookup<Blob>(sha)) != null)
                using (Stream stream = blob.ContentStream)
                using (var outstream = File.Create(destination.FullName))
                        stream.CopyTo(outstream);
        }

        public string HashAndInsertObject(string filename)
        {
            return _repository.ObjectDatabase.CreateBlob(filename).Id.Sha;
        }

        public string AssertValidBranchName(string gitBranchName)
        {
            if (!_repository.Refs.IsValidName("refs/heads/" + gitBranchName))
                throw new GitTfsException("The name specified for the new git branch is not allowed. Choose another one!");
            return gitBranchName;
        }

        public bool CreateBranch(string gitBranchName, string target)
        {
            Reference reference;
            try
            {
                reference = _repository.Refs.Create(gitBranchName, target);
            }
            catch (Exception)
            {
                return false;
            }
            return reference != null;
        }

        public string FindCommitHashByCommitMessage(string patternToFind)
        {
            var regex = new Regex(patternToFind);
            foreach (var branch in _repository.Branches.Where(p => p.IsRemote).ToList())
            {
                var commit = branch.Commits.SingleOrDefault(c => regex.IsMatch(c.Message));
                if (commit != null)
                    return commit.Sha;
            }
            return null;
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
    }
}
