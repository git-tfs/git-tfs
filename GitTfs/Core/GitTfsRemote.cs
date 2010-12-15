using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class GitTfsRemote : IGitTfsRemote
    {
        private static readonly Regex isInDotGit = new Regex("(?:^|/)\\.git(?:/|$)");
        private static readonly Regex treeShaRegex = new Regex("^tree (" + GitTfsConstants.Sha1 + ")");

        private readonly Globals globals;
        private readonly TextWriter stdout;
        private readonly RemoteOptions remoteOptions;
        private long? maxChangesetId;
        private string maxCommitHash;

        public GitTfsRemote(RemoteOptions remoteOptions, Globals globals, ITfsHelper tfsHelper, TextWriter stdout)
        {
            this.remoteOptions = remoteOptions;
            this.globals = globals;
            this.stdout = stdout;
            Tfs = tfsHelper;
        }

        public string Id { get; set; }
        public string TfsRepositoryPath { get; set; }
        public string IgnoreRegexExpression { get; set; }
        public IGitRepository Repository { get; set; }
        public ITfsHelper Tfs { get; set; }

        public long MaxChangesetId
        {
            get { InitHistory(); return maxChangesetId.Value; }
            set { maxChangesetId = value; }
        }

        public string MaxCommitHash
        {
            get { InitHistory(); return maxCommitHash; }
            set { maxCommitHash = value; }
        }

        private void InitHistory()
        {
            if (maxChangesetId == null)
            {
                var mostRecentUpdate = Repository.GetParentTfsCommits(RemoteRef).FirstOrDefault();
                if (mostRecentUpdate != null)
                {
                    MaxCommitHash = mostRecentUpdate.GitCommit;
                    MaxChangesetId = mostRecentUpdate.ChangesetId;
                }
                else
                {
                    MaxChangesetId = 0;
                }
            }
        }

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

        private string WorkingDirectory
        {
            get
            {
                return Path.Combine(Dir, "workspace");
            }
        }

        public bool ShouldSkip(string path)
        {
            return IsInDotGit(path) ||
                   IsIgnored(path, IgnoreRegexExpression) ||
                   IsIgnored(path, remoteOptions.IgnoreRegex);
        }

        private bool IsIgnored(string path, string expression)
        {
            return expression != null && new Regex(expression).IsMatch(path);
        }

        private bool IsInDotGit(string path)
        {
            return isInDotGit.IsMatch(path);
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            if(!tfsPath.StartsWith(TfsRepositoryPath,StringComparison.InvariantCultureIgnoreCase)) return null;
            tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);
            while (tfsPath.StartsWith("/"))
                tfsPath = tfsPath.Substring(1);
            return tfsPath;
        }

        public void Fetch(Dictionary<long, string> mergeInfo)
        {
            foreach (var changeset in FetchChangesets())
            {
                AssertTemporaryIndexClean(MaxCommitHash);
                var log = Apply(MaxCommitHash, changeset);
                if(mergeInfo.ContainsKey(changeset.Summary.ChangesetId))
                    log.CommitParents.Add(mergeInfo[changeset.Summary.ChangesetId]);
                UpdateRef(Commit(log), changeset.Summary.ChangesetId);
                DoGcIfNeeded();
            }
        }

        public void QuickFetch()
        {
            var changeset = Tfs.GetLatestChangeset(this);
            AssertTemporaryIndexEmpty();
            var log = CopyTree(MaxCommitHash, changeset);
            UpdateRef(Commit(log), changeset.Summary.ChangesetId);
            DoGcIfNeeded();
        }

        private IEnumerable<ITfsChangeset> FetchChangesets()
        {
            Trace.WriteLine(RemoteRef + ": Getting changesets from " + (MaxChangesetId + 1) + " to current ...", "info");
            var changesets = Tfs.GetChangesets(TfsRepositoryPath, MaxChangesetId + 1, this);
            changesets = changesets.OrderBy(cs => cs.Summary.ChangesetId);
            return changesets;
        }

        private void UpdateRef(string commitHash, long changesetId)
        {
            MaxCommitHash = commitHash;
            MaxChangesetId = changesetId;
            Repository.CommandNoisy("update-ref", "-m", "C" + MaxChangesetId, RemoteRef, MaxCommitHash);
            Repository.CommandNoisy("update-ref", TagPrefix + "C" + MaxChangesetId, MaxCommitHash);
            LogCurrentMapping();
        }

        private void LogCurrentMapping()
        {
            stdout.WriteLine("C" + MaxChangesetId + " = " + MaxCommitHash);
        }

        private string TagPrefix
        {
            get { return "refs/tags/tfs/" + Id + "/"; }
        }

        public string RemoteRef
        {
            get { return "refs/remotes/tfs/" + Id; }
        }

        private void DoGcIfNeeded()
        {
            Trace.WriteLine("GC Countdown: " + globals.GcCountdown);
            if(--globals.GcCountdown < 0)
            {
                globals.GcCountdown = globals.GcPeriod;
                Repository.CommandNoisy("gc", "--auto");
            }
        }

        private void AssertTemporaryIndexClean(string treeish)
        {
            if(string.IsNullOrEmpty(treeish))
            {
                AssertTemporaryIndexEmpty();
                return;
            }
            WithTemporaryIndex(() => AssertIndexClean(treeish));
        }

        private void AssertTemporaryIndexEmpty()
        {
            if (File.Exists(IndexFile))
                File.Delete(IndexFile);
        }

        private void AssertIndexClean(string treeish)
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
        }

        private LogEntry Apply(string lastCommit, ITfsChangeset changeset)
        {
            LogEntry result = null;
            WithTemporaryIndex(
                () => GitIndexInfo.Do(Repository, index => result = changeset.Apply(lastCommit, index)));
            WithTemporaryIndex(
                () => result.Tree = Repository.CommandOneline("write-tree"));
            if(!String.IsNullOrEmpty(lastCommit)) result.CommitParents.Add(lastCommit);
            return result;
        }

        private LogEntry CopyTree(string lastCommit, ITfsChangeset changeset)
        {
            LogEntry result = null;
            WithTemporaryIndex(
                () => GitIndexInfo.Do(Repository, index => result = changeset.CopyTree(index)));
            WithTemporaryIndex(
                () => result.Tree = Repository.CommandOneline("write-tree"));
            if (!String.IsNullOrEmpty(lastCommit)) result.CommitParents.Add(lastCommit);
            return result;
        }

        private string Commit(LogEntry logEntry)
        {
            string commitHash = null;
            WithCommitHeaderEnv(logEntry, () => commitHash = WriteCommit(logEntry));
            // TODO (maybe): StoreChangesetMetadata(commitInfo);
            return commitHash;
        }

        private string WriteCommit(LogEntry logEntry)
        {
            // TODO (maybe): encode logEntry.Log according to 'git config --get i18n.commitencoding', if specified
            //var commitEncoding = Repository.CommandOneline("config", "i18n.commitencoding");
            //var encoding = LookupEncoding(commitEncoding) ?? Encoding.UTF8;
            string commitHash = null;
            Repository.CommandInputOutputPipe((procIn, procOut) =>
                                                  {
                                                      procIn.WriteLine(logEntry.Log);
                                                      procIn.WriteLine(GitTfsConstants.TfsCommitInfoFormat, Tfs.Url,
                                                                       TfsRepositoryPath, logEntry.ChangesetId);
                                                      procIn.Close();
                                                      commitHash = ParseCommitInfo(procOut.ReadToEnd());
                                                  }, BuildCommitCommand(logEntry));
            return commitHash;
        }

        private string[] BuildCommitCommand(LogEntry logEntry)
        {
            var tree = logEntry.Tree ?? GetTemporaryIndexTreeSha();
            tree.AssertValidSha();
            var commitCommand = new List<string> { "commit-tree", tree };
            foreach (var parent in logEntry.CommitParents)
            {
                commitCommand.Add("-p");
                commitCommand.Add(parent);
            }
            return commitCommand.ToArray();
        }

        private string GetTemporaryIndexTreeSha()
        {
            string tree = null;
            WithTemporaryIndex(() => tree = Repository.CommandOneline("write-tree"));
            return tree;
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

        public void Shelve(string shelvesetName, string head, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies)
        {
            Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset,
                              workspace => Shelve(shelvesetName, head, parentChangeset, evaluateCheckinPolicies, workspace));
        }

        public bool HasShelveset(string shelvesetName)
        {
            return Tfs.HasShelveset(shelvesetName);
        }

        private void Shelve(string shelvesetName, string head, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parentChangeset, workspace);
            workspace.Shelve(shelvesetName, evaluateCheckinPolicies);
        }

        public void CheckinTool(string head, TfsChangesetInfo parentChangeset)
        {
            Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset,
                              workspace => CheckinTool(head, parentChangeset, workspace));
        }

        private void CheckinTool(string head, TfsChangesetInfo parentChangeset, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parentChangeset,workspace);
            workspace.CheckinTool();
        }

        private void PendChangesToWorkspace(string head, TfsChangesetInfo parentChangeset, ITfsWorkspace workspace)
        {
            foreach (var change in Repository.GetChangedFiles(parentChangeset.GitCommit, head))
            {
                change.Apply(workspace);
            }
        }

        public long Checkin(string head, TfsChangesetInfo parentChangeset)
        {
            var changeset = 0L;
            Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset,
                              workspace => changeset = Checkin(head, parentChangeset, workspace));
            return changeset;
        }

        private long Checkin(string head, TfsChangesetInfo parentChangeset, ITfsWorkspace workspace)
        {
            foreach (var change in Repository.GetChangedFiles(parentChangeset.GitCommit, head))
            {
                change.Apply(workspace);
            }
            return workspace.Checkin();
        }
    }
}
