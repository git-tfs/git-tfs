using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitSharp.Core;
using Sep.Git.Tfs.Util;
using StructureMap;
using FileMode=GitSharp.Core.FileMode;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        private static readonly Regex configLineRegex = new Regex("^tfs-remote\\.(?<id>[^.]+)\\.(?<key>[^.=]+)=(?<value>.*)$");
        private IDictionary<string, IGitTfsRemote> _cachedRemotes;
        private Repository _repository;

        public GitRepository(TextWriter stdout, string gitDir) : base(stdout)
        {
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
                throw new Exception("Unable to locate git-tfs remote with id = " + remoteId, e);
            }
        }

        public IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            try
            {
                var allRemotes = GetTfsRemotes();
                return
                    allRemotes.Values.First(
                        remote => remote.Tfs.Url == tfsUrl && remote.TfsRepositoryPath == tfsRepositoryPath);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to locate git-tfs remote with url = " + tfsUrl + ", repo = " +
                                    tfsRepositoryPath, e);
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
                case "username":
                    remote.Tfs.Username = value;
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
            var remote = ObjectFactory.GetInstance<IGitTfsRemote>();
            remote.Repository = this;
            remote.Id = id;
            return remote;
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
                var commitInfo = ObjectFactory.GetInstance<TfsChangesetInfo>();
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
            using (var diffOutput = CommandOutputPipe("diff-tree", "-r", from, to))
            {
                while (':' == diffOutput.Read())
                {
                    var builder = ObjectFactory.With("repository").EqualTo(this);
                    builder = builder.With("oldMode").EqualTo(diffOutput.Read(6));
                    diffOutput.Read(1); // a space
                    var newMode = diffOutput.Read(6);
                    builder = builder.With("newMode").EqualTo(newMode);
                    diffOutput.Read(1); // a space
                    builder = builder.With("oldSha").EqualTo(diffOutput.Read(40));
                    diffOutput.Read(1); // a space
                    builder = builder.With("newSha").EqualTo(diffOutput.Read(40));
                    diffOutput.Read(1); // a space
                    var changeType = diffOutput.Read(1);
                    diffOutput.Read(1); // tab
                    builder = builder.With("path").EqualTo(diffOutput.ReadLine().Trim());

                    if(FileMode.GitLink == newMode.ToFileMode())
                        continue;

                    IGitChangedFile change;
                    try
                    {
                        change = builder.GetInstance<IGitChangedFile>(changeType);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unable to handle change type " + changeType + ".", e);
                    }
                    yield return change;
                }
            }
        }

        public string GetChangeSummary(string from, string to)
        {
            string summary = "";
            CommandOutputPipe(stdout => summary = stdout.ReadToEnd(),
                              "diff-tree", "--shortstat", from, to);
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
}
