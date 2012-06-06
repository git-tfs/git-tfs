using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Commands;
using StructureMap;
using FileMode = LibGit2Sharp.Mode;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        private readonly IContainer _container;
        private readonly Globals _globals;
        private static readonly Regex configLineRegex = new Regex("^tfs-remote\\.(?<id>[^.]+)\\.(?<key>[^.=]+)=(?<value>.*)$");
        private IDictionary<string, IGitTfsRemote> _cachedRemotes;
        private Repository _repository;

        public GitRepository(TextWriter stdout, string gitDir, IContainer container, Globals globals)
            : base(stdout, container)
        {
            _container = container;
            _globals = globals;
            GitDir = gitDir;
            _repository = new LibGit2Sharp.Repository(GitDir);
        }

        ~GitRepository()
        {
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

        public IEnumerable<IGitTfsRemote> ReadAllTfsRemotes()
        {
            return GetTfsRemotes().Values;
        }

        public IGitTfsRemote ReadTfsRemote(string remoteId)
        {
            if (!HasRemote(remoteId))
                throw new GitTfsException("Unable to locate git-tfs remote with id = " + remoteId)
                    .WithRecommendation("Try using `git tfs bootstrap` to auto-init TFS remotes.");
            return GetTfsRemotes()[remoteId];
        }

        private IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath, bool includeStubRemotes)
        {
            var allRemotes = GetTfsRemotes();
            var matchingRemotes =
                allRemotes.Values.Where(
                    remote => remote.Tfs.MatchesUrl(tfsUrl) && remote.TfsRepositoryPath == tfsRepositoryPath);
            switch (matchingRemotes.Count())
            {
                case 0:
                    if (!includeStubRemotes)
                        throw new GitTfsException("Unable to locate a remote for <" + tfsUrl + ">" + tfsRepositoryPath)
                            .WithRecommendation("Try using `git tfs bootstrap` to auto-init TFS remotes.")
                            .WithRecommendation("Try setting a legacy-url for an existing remote.");
                    return new DerivedGitTfsRemote(tfsUrl, tfsRepositoryPath);
                case 1:
                    return matchingRemotes.First();
                default:
                    Trace.WriteLine("More than one remote matched!");
                    goto case 1;
            }
        }

        private IDictionary<string, IGitTfsRemote> GetTfsRemotes()
        {
            return _cachedRemotes ?? (_cachedRemotes = ReadTfsRemotes());
        }

        private IDictionary<string, IGitTfsRemote> ReadTfsRemotes()
        {
            var remotes = new Dictionary<string, IGitTfsRemote>();
            CommandOutputPipe(stdout => ParseRemoteConfig(stdout, remotes), "config", "--list");
            return remotes;
        }

        public bool HasRemote(string remoteId)
        {
            return GetTfsRemotes().ContainsKey(remoteId);
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

        public void CreateTfsRemote(string remoteId, TfsChangesetInfo tfsHead, RemoteOptions remoteOptions)
        {
            CreateTfsRemote(remoteId, tfsHead.Remote.TfsUrl, tfsHead.Remote.TfsRepositoryPath, remoteOptions);
            ReadTfsRemote(remoteId).UpdateRef(tfsHead.GitCommit, tfsHead.ChangesetId);
        }

        public void CreateTfsRemote(string remoteId, string tfsUrl, string tfsRepositoryPath, RemoteOptions remoteOptions)
        {
            if (HasRemote(remoteId))
                throw new GitTfsException("A remote with id \"" + remoteId + "\" already exists.");

            if (remoteOptions != null)
            {
                if (remoteOptions.NoMetaData) SetTfsConfig(remoteId, "no-meta-data", 1);
                if (remoteOptions.IgnoreRegex != null) SetTfsConfig(remoteId, "ignore-paths", remoteOptions.IgnoreRegex);
                if (!string.IsNullOrEmpty(remoteOptions.Username)) SetTfsConfig(remoteId, "username", remoteOptions.Username);
                if (!string.IsNullOrEmpty(remoteOptions.Password)) SetTfsConfig(remoteId, "password", remoteOptions.Password);
            }

            SetTfsConfig(remoteId, "url", tfsUrl);
            SetTfsConfig(remoteId, "repository", tfsRepositoryPath);
            SetTfsConfig(remoteId, "fetch", "refs/remotes/" + remoteId + "/master");

            Directory.CreateDirectory(Path.Combine(GitDir, "tfs"));
            _cachedRemotes = null;
        }

        private void SetTfsConfig(string remoteId, string subkey, object value)
        {
            this.SetConfig(_globals.RemoteConfigKey(remoteId, subkey), value);
        }

        private void ParseRemoteConfig(TextReader stdout, IDictionary<string, IGitTfsRemote> remotes)
        {
            string line;
            while ((line = stdout.ReadLine()) != null)
            {
                TryParseRemoteConfigLine(line, remotes);
            }
            foreach (var gitTfsRemotePair in remotes)
            {
                var remote = gitTfsRemotePair.Value;
                remote.EnsureTfsAuthenticated();
            }
        }

        private void TryParseRemoteConfigLine(string line, IDictionary<string, IGitTfsRemote> remotes)
        {
            var match = configLineRegex.Match(line);
            if (match.Success)
            {
                var key = match.Groups["key"].Value;
                var value = match.Groups["value"].Value;
                var remoteId = match.Groups["id"].Value;
                var remote = remotes.ContainsKey(remoteId)
                                 ? remotes[remoteId]
                                 : (remotes[remoteId] = BuildRemote(remoteId));
                try
                {
                    SetRemoteConfigValue(remote, key, value);
                }
                catch(Exception e)
                {
                    throw new GitTfsException("Malformed value for " + key + ": " + value, e);
                }
            }
        }

        private IGitTfsRemote BuildRemote(string id)
        {
            var remote = _container.GetInstance<IGitTfsRemote>();
            remote.Repository = this;
            remote.Id = id;
            return remote;
        }

        private void SetRemoteConfigValue(IGitTfsRemote remote, string key, string value)
        {
            switch (key)
            {
                case "url":
                    remote.TfsUrl = value;
                    break;
                case "legacy-urls":
                    remote.Tfs.LegacyUrls = value.Split(',');
                    break;
                case "repository":
                    remote.TfsRepositoryPath = value;
                    break;
                case "ignore-paths":
                    remote.IgnoreRegexExpression = value;
                    break;
                case "username":
                    remote.TfsUsername = value;
                    break;
                case "password":
                    remote.TfsPassword = value;
                    break;
                case "autotag":
                    remote.Autotag = bool.Parse(value);
                    break;
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

        private void FindTfsCommits(TextReader stdout, ICollection<TfsChangesetInfo> tfsCommits, bool includeStubRemotes)
        {
            string currentCommit = null;
            string line;
            while (null != (line = stdout.ReadLine()))
            {
                var match = GitTfsConstants.CommitRegex.Match(line);
                if (match.Success)
                {
                    currentCommit = match.Groups[1].Value;
                    continue;
                }
                var changesetInfo = TryParseChangesetInfo(line, currentCommit, includeStubRemotes);
                if (changesetInfo != null)
                {
                    tfsCommits.Add(changesetInfo);
                    currentCommit = null;
                }
            }
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
            System.Text.StringBuilder message = new System.Text.StringBuilder();
            foreach (LibGit2Sharp.Commit comm in
                _repository.Commits.QueryBy(new LibGit2Sharp.Filter { Since = head, Until = parentCommitish }))
            {
                message.AppendLine(comm.Message);
            }
            return message.ToString();
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
                    entries[item.Path] = new GitObject
                    {
                        Mode = item.Mode.ToModeString(),
                        Sha = item.Target.Sha,
                        ObjectType = item.Type.ToString().ToLower(),
                        Path = item.Path,
                        Commit = commit
                    };
                }
            }
        }

        public IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to)
        {
            using (var diffOutput = CommandOutputPipe("diff-tree", "-r", "-M", from, to))
            {
                string line;
                while (null != (line = diffOutput.ReadLine()))
                {
                    var change = GitChangeInfo.Parse(line);

                    if (FileMode.GitLink == change.NewMode)
                        continue;

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
    }
}
