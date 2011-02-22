using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitSharp.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;
using FileMode=GitSharp.Core.FileMode;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        private readonly IContainer _container;
        private static readonly Regex configLineRegex = new Regex("^tfs-remote\\.(?<id>[^.]+)\\.(?<key>[^.=]+)=(?<value>.*)$");
        private IDictionary<string, IGitTfsRemote> _cachedRemotes;
        private Repository _repository;

        public GitRepository(TextWriter stdout, string gitDir, IContainer container) : base(stdout, container)
        {
            _container = container;
            GitDir = gitDir;
            _repository = new Repository(new DirectoryInfo(gitDir));
        }

        private string GitDir { get; set; }
        public string WorkingCopyPath { get; set; }
        public string WorkingCopySubdir { get; set; }

        protected override Process Start(string [] command, Action<ProcessStartInfo> initialize)
        {
            return base.Start(command, initialize.And(SetUpPaths));
        }

        private void SetUpPaths(ProcessStartInfo gitCommand)
        {
            if(GitDir != null)
                gitCommand.EnvironmentVariables["GIT_DIR"] = GitDir;
            if(WorkingCopyPath != null)
                gitCommand.WorkingDirectory = WorkingCopyPath;
            if(WorkingCopySubdir != null)
                gitCommand.WorkingDirectory = Path.Combine(gitCommand.WorkingDirectory, WorkingCopySubdir);
        }

        public IEnumerable<IGitTfsRemote> ReadAllTfsRemotes()
        {
            return GetTfsRemotes().Values;
        }

        public IGitTfsRemote ReadTfsRemote(string remoteId)
        {
            try
            {
                return GetTfsRemotes()[remoteId];
            }
            catch(Exception e)
            {
                throw new GitTfsException("Unable to locate git-tfs remote with id = " + remoteId, e)
                    .WithRecommendation("Try using `git tfs bootstrap` to auto-init git-tfs.");
            }
        }

        public IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            var allRemotes = GetTfsRemotes();
            var matchingRemotes =
                allRemotes.Values.Where(
                    remote => remote.Tfs.MatchesUrl(tfsUrl) && remote.TfsRepositoryPath == tfsRepositoryPath);
            switch(matchingRemotes.Count())
            {
                case 0:
                    return new FakeGitTfsRemote(tfsUrl, tfsRepositoryPath);
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
            CommandOutputPipe(stdout => ParseRemoteConfig(stdout, remotes), "config", "-l");
            return remotes;
        }

        private void ParseRemoteConfig(TextReader stdout, IDictionary<string, IGitTfsRemote> remotes)
        {
            string line;
            while ((line = stdout.ReadLine()) != null)
            {
                TryParseRemoteConfigLine(line, remotes);
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
                                 : (remotes[remoteId] = CreateRemote(remoteId));
                SetRemoteConfigValue(remote, key, value);
            }
        }

        private void SetRemoteConfigValue(IGitTfsRemote remote, string key, string value)
        {
            switch (key)
            {
                case "url":
                    remote.Tfs.Url = value;
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
                    //case "fetch":
                    //    remote.??? = value;
                    //    break;
            }
        }

        private IGitTfsRemote CreateRemote(string id)
        {
            var remote = _container.GetInstance<IGitTfsRemote>();
            remote.Repository = this;
            remote.Id = id;
            return remote;
        }

        public GitCommit GetCommit(string commitish)
        {
            return _container.With(_repository.MapCommit(commitish)).GetInstance<GitCommit>();
        }

        public IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head)
        {
            return GetParentTfsCommits(head, new List<string>());
        }

        public IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head, ICollection<string> localCommits)
        {
            var tfsCommits = new List<TfsChangesetInfo>();
            try
            {
                CommandOutputPipe(stdout => FindTfsCommits(stdout, tfsCommits, localCommits),
                  "log", "--no-color", "--pretty=medium", head);
            }
            catch (GitCommandException e)
            {
                Trace.WriteLine("An error occurred while loading head " + head + " (maybe it doesn't exist?): " + e);
            }
            return from commit in tfsCommits
                   group commit by commit.Remote
                   into remotes
                       select remotes.OrderBy(commit => -commit.ChangesetId).First();
        }

        private void FindTfsCommits(TextReader stdout, ICollection<TfsChangesetInfo> tfsCommits, ICollection<string> localCommits)
        {
            string currentCommit = null;
            string line;
            var commitRegex = new Regex("commit (" + GitTfsConstants.Sha1 + ")");
            while (null != (line = stdout.ReadLine()))
            {
                var match = commitRegex.Match(line);
                if (match.Success)
                {
                    if (currentCommit != null) localCommits.Add(currentCommit);
                    currentCommit = match.Groups[1].Value;
                    continue;
                }
                var changesetInfo = TryParseChangesetInfo(line, currentCommit);
                if (changesetInfo != null)
                {
                    tfsCommits.Add(changesetInfo);
                    currentCommit = null;
                }
            }
            //stdout.Close();
        }

        private TfsChangesetInfo TryParseChangesetInfo(string gitTfsMetaInfo, string commit)
        {
            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);
            if (match.Success)
            {
                var commitInfo = _container.GetInstance<TfsChangesetInfo>();
                commitInfo.Remote = ReadTfsRemote(match.Groups["url"].Value, match.Groups["repository"].Value);
                commitInfo.ChangesetId = Convert.ToInt32(match.Groups["changeset"].Value);
                commitInfo.GitCommit = commit;
                return commitInfo;
            }
            return null;
        }

        public IDictionary<string, GitObject> GetObjects(string commit)
        {
            var entries = new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
            if (commit != null)
            {
                ParseEntries(entries, Command("ls-tree", "-r", "-z", commit), commit);
                ParseEntries(entries, Command("ls-tree", "-r", "-d", "-z", commit), commit);
            }
            return entries;
        }

        private void ParseEntries(IDictionary<string, GitObject> entries, string treeInfo, string commit)
        {
            foreach (var treeEntry in treeInfo.Split('\0'))
            {
                var gitObject = MakeGitObject(commit, treeEntry);
                if(gitObject != null)
                {
                    entries[gitObject.Path] = gitObject;
                }
            }
        }

        private GitObject MakeGitObject(string commit, string treeInfo)
        {
            var treeRegex =
                new Regex(@"\A(?<mode>\d{6}) (?<type>blob|tree) (?<sha>" + GitTfsConstants.Sha1 + @")\t(?<path>.*)");
            var match = treeRegex.Match(treeInfo);
            return !match.Success ? null : new GitObject
                                               {
                                                   Mode = match.Groups["mode"].Value,
                                                   Sha = match.Groups["sha"].Value,
                                                   ObjectType = match.Groups["type"].Value,
                                                   Path = match.Groups["path"].Value,
                                                   Commit = commit
                                               };
        }

        public IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to)
        {
            using (var diffOutput = CommandOutputPipe("diff-tree", "-r", "-M", from, to))
            {
                string line;
                while(null != (line = diffOutput.ReadLine()))
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
            return change.ToGitChangedFile(_container.With((IGitRepository)this));
        }

        public string GetChangeSummary(string from, string to)
        {
            string summary = "";
            CommandOutputPipe(stdout => summary = stdout.ReadToEnd(),
                              "diff-tree", "--shortstat", "-M", from, to);
            return summary;
        }

        public void GetBlob(string sha, string outputFile)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            CommandOutputPipe(stdout => Copy(stdout, outputFile), "cat-file", "-p", sha);
        }

        private void Copy(TextReader stdout, string file)
        {
            var stdoutStream = ((StreamReader) stdout).BaseStream;
            using(var destination = File.Create(file))
            {
                stdoutStream.CopyTo(destination);
            }
        }

        public string HashAndInsertObject(string filename)
        {
            var writer = new ObjectWriter(_repository);
            var objectId = writer.WriteBlob(new FileInfo(filename));
            return objectId.Name;
        }

        public string HashAndInsertObject(Stream file)
        {
            var writer = new ObjectWriter(_repository);
            var objectId = writer.WriteBlob(file.Length, file);
            return objectId.Name;
        }
    }

    public class FakeGitTfsRemote : IGitTfsRemote
    {
        private readonly string _tfsUrl;
        private readonly string _tfsRepositoryPath;

        public FakeGitTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            _tfsUrl = tfsUrl;
            _tfsRepositoryPath = tfsRepositoryPath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (FakeGitTfsRemote)) return false;
            return Equals((FakeGitTfsRemote) obj);
        }

        private bool Equals(FakeGitTfsRemote other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._tfsUrl, _tfsUrl) && Equals(other._tfsRepositoryPath, _tfsRepositoryPath);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_tfsUrl != null ? _tfsUrl.GetHashCode() : 0)*397) ^ (_tfsRepositoryPath != null ? _tfsRepositoryPath.GetHashCode() : 0);
            }
        }

        public static bool operator ==(FakeGitTfsRemote left, FakeGitTfsRemote right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FakeGitTfsRemote left, FakeGitTfsRemote right)
        {
            return !Equals(left, right);
        }

        public string Id
        {
            get { return "(deduced)"; }
            set { throw new NotImplementedException(); }
        }

        public string TfsRepositoryPath
        {
            get { return _tfsRepositoryPath; }
            set { throw new NotImplementedException(); }
        }

        public string IgnoreRegexExpression
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IGitRepository Repository
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ITfsHelper Tfs
        {
            get { return new FakeTfsHelper(this); }
            set { throw new NotImplementedException(); }
        }

        class FakeTfsHelper : ITfsHelper
        {
            private readonly FakeGitTfsRemote _fakeGitTfsRemote;

            public FakeTfsHelper(FakeGitTfsRemote fakeGitTfsRemote)
            {
                _fakeGitTfsRemote = fakeGitTfsRemote;
            }

            public string TfsClientLibraryVersion
            {
                get { throw new NotImplementedException(); }
            }

            public string Url
            {
                get { return _fakeGitTfsRemote._tfsUrl; }
                set { throw new NotImplementedException(); }
            }

            public string[] LegacyUrls
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote)
            {
                throw new NotImplementedException();
            }

            public void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
            {
                throw new NotImplementedException();
            }

            public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
            {
                throw new NotImplementedException();
            }

            public IIdentity GetIdentity(string username)
            {
                throw new NotImplementedException();
            }

            public ITfsChangeset GetLatestChangeset(GitTfsRemote remote)
            {
                throw new NotImplementedException();
            }

            public ITfsChangeset GetChangeset(int changesetId, GitTfsRemote remote)
            {
                throw new NotImplementedException();
            }

            public IChangeset GetChangeset(int changesetId)
            {
                throw new NotImplementedException();
            }

            public bool MatchesUrl(string tfsUrl)
            {
                throw new NotImplementedException();
            }

            public bool HasShelveset(string shelvesetName)
            {
                throw new NotImplementedException();
            }

            public bool CanShowCheckinDialog
            {
                get { throw new NotImplementedException(); }
            }

            public long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
            {
                throw new NotImplementedException();
            }

            public void CleanupWorkspaces(string workingDirectory)
            {
                throw new NotImplementedException();
            }

            public void PrepareForCheckinEvaluation()
            {
                throw new NotImplementedException();
            }
        }

        public long MaxChangesetId
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string MaxCommitHash
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string RemoteRef
        {
            get { throw new NotImplementedException(); }
        }

        public bool ShouldSkip(string path)
        {
            throw new NotImplementedException();
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            throw new NotImplementedException();
        }

        public void Fetch(Dictionary<long, string> mergeInfo)
        {
            throw new NotImplementedException();
        }

        public void QuickFetch()
        {
            throw new NotImplementedException();
        }

        public void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies)
        {
            throw new NotImplementedException();
        }

        public bool HasShelveset(string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public long CheckinTool(string head, TfsChangesetInfo parentChangeset)
        {
            throw new NotImplementedException();
        }

        public long Checkin(string treeish, TfsChangesetInfo parentChangeset)
        {
            throw new NotImplementedException();
        }

        public void CleanupWorkspace()
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetChangeset(long changesetId)
        {
            throw new NotImplementedException();
        }
    }
}
