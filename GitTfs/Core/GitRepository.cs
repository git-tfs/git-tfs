using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitSharp.Core;
using Sep.Git.Tfs.Commands;
using StructureMap;
using FileMode = GitSharp.Core.FileMode;

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
            _repository = new Repository(new DirectoryInfo(gitDir));
        }

        private string GitDir { get; set; }
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
            CommandOutputPipe(stdout => ParseRemoteConfig(stdout, remotes), "config", "-l");
            return remotes;
        }

        public bool HasRemote(string remoteId)
        {
            return GetTfsRemotes().ContainsKey(remoteId);
        }

        public void CreateTfsRemote(string remoteId, TfsChangesetInfo tfsHead)
        {
            CreateTfsRemote(remoteId, tfsHead.Remote.TfsUrl, tfsHead.Remote.TfsRepositoryPath, null);
            ReadTfsRemote(remoteId).UpdateRef(tfsHead.GitCommit, tfsHead.ChangesetId);
        }

        public void CreateTfsRemote(string remoteId, string tfsUrl, string tfsRepositoryPath, RemoteOptions remoteOptions)
        {
            if (HasRemote(remoteId))
                throw new GitTfsException("A remote with id \"" + remoteId + "\" already exists.");

            SetTfsConfig(remoteId, "url", tfsUrl);
            SetTfsConfig(remoteId, "repository", tfsRepositoryPath);
            SetTfsConfig(remoteId, "fetch", "refs/remotes/" + remoteId + "/master");
            if (remoteOptions != null)
            {
                if (remoteOptions.NoMetaData) SetTfsConfig(remoteId, "no-meta-data", 1);
                if (remoteOptions.IgnoreRegex != null) SetTfsConfig(remoteId, "ignore-paths", remoteOptions.IgnoreRegex);
            }

            Directory.CreateDirectory(Path.Combine(this.GitDir, "tfs"));
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
                SetRemoteConfigValue(remote, key, value);
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
                //case "fetch":
                //    remote.??? = value;
                //    break;
            }
        }

        public GitCommit GetCommit(string commitish)
        {
            return _container.With(_repository.MapCommit(commitish)).GetInstance<GitCommit>();
        }

        public IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head)
        {
            return GetParentTfsCommits(head, false);
        }

        public IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head, bool includeStubRemotes)
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
            return from commit in tfsCommits
                   group commit by commit.Remote
                       into remotes
                       select remotes.OrderBy(commit => -commit.ChangesetId).First();
        }

        private void FindTfsCommits(TextReader stdout, ICollection<TfsChangesetInfo> tfsCommits, bool includeStubRemotes)
        {
            string currentCommit = null;
            string line;
            var commitRegex = new Regex("commit (" + GitTfsConstants.Sha1 + ")");
            while (null != (line = stdout.ReadLine()))
            {
                var match = commitRegex.Match(line);
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
                ParseEntries(entries, Command("ls-tree", "-r", "-z", commit), commit);
                ParseEntries(entries, Command("ls-tree", "-r", "-d", "-z", commit), commit);
            }
            return entries;
        }

        public Dictionary<string, GitObject> GetObjects()
        {
            return new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string GetCommitMessage(string head, string parentCommitish)
        {
            string message = string.Empty;
            using (var logMessage = CommandOutputPipe("log", parentCommitish + ".." + head))
            {
                string line;
                while (null != (line = logMessage.ReadLine()))
                {
                    if (!line.StartsWith("   "))
                        continue;
                    message += line.TrimStart() + Environment.NewLine;
                }
            }
            return message;
        }

        private void ParseEntries(IDictionary<string, GitObject> entries, string treeInfo, string commit)
        {
            foreach (var treeEntry in treeInfo.Split('\0'))
            {
                var gitObject = MakeGitObject(commit, treeEntry);
                if (gitObject != null)
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
            return change.ToGitChangedFile(_container.With((IGitRepository)this));
        }

        public string GetChangeSummary(string from, string to)
        {
            string summary = "";
            CommandOutputPipe(stdout => summary = stdout.ReadToEnd(),
                              "diff-tree", "--shortstat", "-M", from, to);
            return summary;
        }

        public bool WorkingCopyHasUnstagedOrUncommitedChanges
        {
            get
            {
                string pendingChanges = "";
                CommandOutputPipe(stdout => pendingChanges = stdout.ReadToEnd(),
                                  "diff-index", "--name-status", "-M", "HEAD");
                return !string.IsNullOrEmpty(pendingChanges);
            }
        }

        public void GetBlob(string sha, string outputFile)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            CommandOutputPipe(stdout => Copy(stdout, outputFile), "cat-file", "-p", sha);
        }

        private void Copy(TextReader stdout, string file)
        {
            var stdoutStream = ((StreamReader)stdout).BaseStream;
            using (var destination = File.Create(file))
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

        public string HashAndInsertObject(Stream file, long length)
        {
            var writer = new ObjectWriter(_repository);
            var objectId = writer.WriteBlob(length, file);
            return objectId.Name;
        }
    }
}
