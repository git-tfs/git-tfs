using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{
    public class GitTfsRemote
    {
        private readonly Globals globals;
        private readonly RemoteOptions remoteOptions;

        public GitTfsRemote(RemoteOptions remoteOptions, Globals globals, ITfsHelper tfsHelper)
        {
            this.remoteOptions = remoteOptions;
            this.globals = globals;
            Tfs = tfsHelper;
        }

        public string Id { get; set; }
        public string TfsRepositoryPath { get; set; }
        public string IgnoreRegex { get; set; }
        // TODO: Initialize MaxChangesetId and MaxCommitHash
        public long MaxChangesetId { get; set; }
        public string MaxCommitHash { get; set; }
        public IGitRepository Repository { get; set; }
        public ITfsHelper Tfs { get; set; }

        private string Dir
        {
            get
            {
                return Ext.CombinePaths(globals.GitDir, "tfs", Id);
            }
        }

        private string IndexFile
        {
            get
            {
                return Path.Combine(Dir, "index");
            }
        }

        public bool IsIgnored(string path)
        {
            var inDotGit = new Regex("(?:^|/)\\.git(?:/|$)");
            if(inDotGit.IsMatch(path)) return true;
            if(IgnoreRegex != null && new Regex(IgnoreRegex).IsMatch(path)) return true;
            if(remoteOptions.IgnoreRegex != null && new Regex(remoteOptions.IgnoreRegex).IsMatch(path)) return true;
            return false;
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            if(!tfsPath.StartsWith(TfsRepositoryPath)) return null;
            tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);
            while (tfsPath.StartsWith("/"))
                tfsPath = tfsPath.Substring(1);
            return tfsPath;
        }

        public void Fetch()
        {
            //var ignoreRegexPattern = remoteOptions.IgnoreRegex ?? IgnoreRegex;
            //var ignoreRegex = ignoreRegexPattern == null ? null : new Regex(ignoreRegexPattern);
            foreach (var changeset in Tfs.GetChangesets(this).OrderBy(cs => cs.Summary.ChangesetId))
            {
                if (MaxCommitHash != null)
                    AssertIndexClean(MaxCommitHash);
                var log = Apply(MaxCommitHash, changeset);
                MaxCommitHash = Commit(log);
                Trace.WriteLine("C" + changeset.Summary.ChangesetId + " = " + MaxCommitHash);
                MaxChangesetId = changeset.Summary.ChangesetId;
                Repository.CommandNoisy("update-ref", "-m", "C" + MaxChangesetId, RemoteRef, MaxCommitHash);
                DoGcIfNeeded();
            }
        }

        private string RemoteRef
        {
            get { return "refs/remotes/tfs/" + Id; }
        }

        private void DoGcIfNeeded()
        {
            if(--globals.GcCountdown == 0)
            {
                globals.GcCountdown = globals.GcPeriod;
                Repository.CommandNoisy("gc", "--auto");
            }
        }

        private void AssertIndexClean(string treeish)
        {
            if(string.IsNullOrEmpty(treeish)) throw new ArgumentNullException("treeish");
            var treeShaRegex = new Regex("^tree (" + GitTfsConstants.Sha1 + ")");
            WithTemporaryIndex(() =>
            {
                if (!File.Exists(IndexFile)) Repository.CommandNoisy("read-tree", treeish);
                var currentTree = Repository.CommandOneline("write-tree");
                var expectedCommitInfo = Repository.Command("cat-file", "commit", treeish);
                var expectedCommitTree = treeShaRegex.Match(expectedCommitInfo).Groups[1].Value;
                if (expectedCommitTree != currentTree)
                {
                    Trace.WriteLine("Index mismatch: " + expectedCommitTree + " != " + currentTree);
                    Trace.WriteLine("rereading " + treeish);
                    File.Delete(IndexFile);
                    Repository.CommandNoisy("read-tree", treeish);
                    currentTree = Repository.CommandOneline("write-tree");
                    if (expectedCommitTree != currentTree)
                    {
                        throw new Exception("Unable to create a clean temporary index: trees (" + treeish + ") " + expectedCommitTree + " != " + currentTree);
                    }
                }
            });
        }

        private LogEntry Apply(string lastCommit, ITfsChangeset changeset)
        {
            LogEntry result = null;
            WithTemporaryIndex(
                () => GitIndexInfo.Do(Repository, index => result = changeset.Apply(this, lastCommit, index)));
            WithTemporaryIndex(
                () => result.Tree = Repository.CommandOneline("write-tree"));
            if(!String.IsNullOrEmpty(lastCommit)) result.CommitParents.Add(lastCommit);
            return result;
        }

        private string Commit(LogEntry logEntry)
        {
            string commitHash = null;
            var sha1OnlyRegex = new Regex("^" + GitTfsConstants.Sha1 + "$");
            WithCommitHeaderEnv(logEntry, () => {
                                                    var tree = logEntry.Tree;
                                                    if(tree == null)
                                                        WithTemporaryIndex(() => tree = Repository.CommandOneline("write-tree"));
                                                    if (!sha1OnlyRegex.IsMatch(tree))
                                                        throw new Exception("Tree is not a valid sha1: " + tree);
                                                    var commitCommand = new List<string> { "commit-tree", tree };
                                                    foreach(var parent in logEntry.CommitParents)
                                                    {
                                                        commitCommand.Add("-p");
                                                        commitCommand.Add(parent);
                                                    }
                                                    // encode logEntry.Log according to 'git config --get i18n.commitencoding', if specified
                                                    //var commitEncoding = Repository.CommandOneline("config", "i18n.commitencoding");
                                                    //var encoding = LookupEncoding(commitEncoding) ?? Encoding.UTF8;
                                                    Repository.CommandInputOutputPipe((stdin, stdout) => {
                                                                                                             // turn off auto-flush to get rid of the 'using'?
                                                                                                             stdin.WriteLine(logEntry.Log);
                                                                                                             stdin.WriteLine(GitTfsConstants.TfsCommitInfoFormat, Tfs.Url, TfsRepositoryPath, logEntry.ChangesetId);
                                                                                                             stdin.Close();
                                                                                                             commitHash = ParseCommitInfo(stdout.ReadToEnd());
                                                    }, commitCommand.ToArray());
            });
            // TODO: StoreChangesetMetadata(commitInfo);
            return commitHash;
        }

        private string ParseCommitInfo(string commitTreeOutput)
        {
            return commitTreeOutput.Trim();
        }

        //private Encoding LookupEncoding(string encoding)
        //{
        //    if(encoding == null)
        //        return null;
        //    throw new NotImplementedException("Need to implement encoding lookup for " + encoding);
        //}

        private void WithCommitHeaderEnv(LogEntry logEntry, Action action)
        {
            WithTemporaryEnvironment(action, new Dictionary<string, string>
                                                 {
                                                     {"GIT_AUTHOR_NAME", logEntry.AuthorName},
                                                     {"GIT_AUTHOR_EMAIL", logEntry.AuthorEmail},
                                                     {"GIT_AUTHOR_DATE", logEntry.Date.FormatForGit()},
                                                     {"GIT_COMMITTER_DATE", logEntry.Date.FormatForGit()},
                                                     {"GIT_COMMITTER_NAME", logEntry.CommitterName ?? logEntry.AuthorName},
                                                     {"GIT_COMMITTER_EMAIL", logEntry.CommitterEmail ?? logEntry.AuthorEmail}
                                                 });
        }

        private void WithTemporaryIndex(Action action)
        {
            WithTemporaryEnvironment(() =>
                                         {
                                             Directory.CreateDirectory(Path.GetDirectoryName(IndexFile));
                                             action();
                                         }, new Dictionary<string, string> {{"GIT_INDEX_FILE", IndexFile}});
        }

        private void WithTemporaryEnvironment(Action action, IDictionary<string, string> newEnvironment)
        {
            var oldEnvironment = new Dictionary<string, string>();
            PushEnvironment(newEnvironment, oldEnvironment);
            try
            {
                action();
            }
            finally
            {
                PushEnvironment(oldEnvironment);
            }
        }

        private void PushEnvironment(IDictionary<string, string> desiredEnvironment)
        {
            PushEnvironment(desiredEnvironment, new Dictionary<string, string>());
        }

        private void PushEnvironment(IDictionary<string, string> desiredEnvironment, IDictionary<string, string> oldEnvironment)
        {
            foreach(var key in desiredEnvironment.Keys)
            {
                oldEnvironment[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, desiredEnvironment[key]);
            }
        }
    }
}
