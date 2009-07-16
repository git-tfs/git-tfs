using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Util;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        public GitRepository(TextWriter stdout) : base(stdout)
        {
        }

        public string GitDir { get; set; }
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

        public IEnumerable<GitTfsRemote> ReadAllTfsRemotes()
        {
            return ReadTfsRemotes().Values;
        }

        public GitTfsRemote ReadTfsRemote(string remoteId)
        {
            try
            {
                return ReadTfsRemotes()[remoteId];
            }
            catch(Exception e)
            {
                throw new Exception("Unable to locate git-tfs remote with id = " + remoteId, e);
            }
        }

        public GitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            try
            {
                var allRemotes = ReadTfsRemotes();
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

        private IDictionary<string, GitTfsRemote> ReadTfsRemotes()
        {
            var remotes = new Dictionary<string, GitTfsRemote>();
            CommandOutputPipe(stdout => ParseRemoteConfig(stdout, remotes), "config", "-l");
            return remotes;
        }

        private void ParseRemoteConfig(TextReader stdout, IDictionary<string, GitTfsRemote> remotes)
        {
            var configLineRegex = new Regex("^tfs-remote\\.(?<id>[^.]+)\\.(?<key>[^.=]+)=(?<value>.*)$");
            string line;
            while ((line = stdout.ReadLine()) != null)
            {
                var match = configLineRegex.Match(line);
                if (match.Success)
                {
                    var remoteId = match.Groups["id"].Value;
                    var key = match.Groups["key"].Value;
                    var value = match.Groups["value"].Value;
                    var remote = remotes.ContainsKey(remoteId)
                                     ? remotes[remoteId]
                                     : (remotes[remoteId] = CreateRemote(remoteId));
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
                        //case "fetch":
                        //    remote.??? = value;
                        //    break;
                    }
                }
            }
        }

        private GitTfsRemote CreateRemote(string id)
        {
            var remote = ObjectFactory.GetInstance<GitTfsRemote>();
            remote.Repository = this;
            remote.Id = id;
            return remote;
        }

        public TfsChangesetInfo WorkingHeadInfo(string head)
        {
            return WorkingHeadInfo(head, new List<string>());
        }

        public TfsChangesetInfo WorkingHeadInfo(string head, ICollection<string> localCommits)
        {
            try
            {
                TfsChangesetInfo retVal = null;
                CommandOutputPipe(stdout => retVal = ParseFirstTfsCommit(stdout, localCommits),
                  "log", "--no-color", "--first-parent", "--pretty=medium", head);
                return retVal;
            }
            catch (GitCommandException e)
            {
                return null;
            }
        }

        private TfsChangesetInfo ParseFirstTfsCommit(TextReader stdout, ICollection<string> localCommits)
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
                var commitInfo = TryParseChangesetInfo(line, currentCommit);
                if (commitInfo != null)
                    return commitInfo;
            }
            return null;
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

        // This is attractive, but I'm wary of encoding/buffering issues due
        // to pulling BaseStream out of stdin. It also breaks my abstraction of
        // using TextWriter for stdin.
        // An alternative is to write the stream to a temp file, and call the
        // string-based HAIO.
        public string HashAndInsertObject(Stream file)
        {
            using(var tempFile = new TemporaryFile())
            {
                using(var tempStream = File.Create(tempFile))
                {
                    file.CopyTo(tempStream);
                }
                return HashAndInsertObject(tempFile);
            }
        }

        public string HashAndInsertObject(string filename)
        {
            string newHash = null;
            CommandOutputPipe(stdout => newHash = stdout.ReadLine().Trim(),
                "hash-object", "-w", filename);
            return newHash;
        }
    }
}
