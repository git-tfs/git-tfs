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

        public void EnsureTfsAuthenticated()
        {
            Tfs.EnsureAuthenticated();
        }

        public bool IsDerived
        {
            get { return false; }
        }

        public string Id { get; set; }

        public string TfsUrl
        {
            get { return Tfs.Url; }
            set { Tfs.Url = value; }
        }

        public bool Autotag { get; set; }

        public string TfsUsername
        {
            get { return Tfs.Username; }
            set { Tfs.Username = value; }
        }

        public string TfsPassword
        {
            get { return Tfs.Password; }
            set { Tfs.Password = value; }
        }

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
                var mostRecentUpdate = Repository.GetLastParentTfsCommits(RemoteRef).FirstOrDefault();
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

        public void CleanupWorkspace()
        {
            Tfs.CleanupWorkspaces(WorkingDirectory);
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
            if (tfsPath == null) return null;
            if(!tfsPath.StartsWith(TfsRepositoryPath,StringComparison.InvariantCultureIgnoreCase)) return null;
            tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);
            while (tfsPath.StartsWith("/"))
                tfsPath = tfsPath.Substring(1);
            return tfsPath;
        }

        public void Fetch()
        {
            FetchWithMerge(-1);
        }

        public void FetchWithMerge(long mergeChangesetId, params string[] parentCommitsHashes)
        {
            foreach (var changeset in FetchChangesets())
            {
                AssertTemporaryIndexClean(MaxCommitHash);
                var log = Apply(MaxCommitHash, changeset);
                if (changeset.Summary.ChangesetId == mergeChangesetId)
                {
                    foreach (var parent in parentCommitsHashes)
                    {
                        log.CommitParents.Add(parent);
                    }
                }
                UpdateRef(Commit(log), changeset.Summary.ChangesetId);
                DoGcIfNeeded();
            }
        }

        public void Apply(ITfsChangeset changeset, string destinationRef)
        {
            var log = Apply(MaxCommitHash, changeset);
            var commit = Commit(log);
            Repository.CommandNoisy("update-ref", destinationRef, commit);
        }

        public void QuickFetch()
        {
            var changeset = Tfs.GetLatestChangeset(this);
            quickFetch(changeset);
        }

        public void QuickFetch(int changesetId)
        {
            var changeset = Tfs.GetChangeset(changesetId, this);
            quickFetch(changeset);
        }

        private void quickFetch(ITfsChangeset changeset)
        {
            AssertTemporaryIndexEmpty();
            var log = CopyTree(MaxCommitHash, changeset);
            UpdateRef(Commit(log), changeset.Summary.ChangesetId);
            DoGcIfNeeded();
        }

        private IEnumerable<ITfsChangeset> FetchChangesets()
        {
            Trace.WriteLine(RemoteRef + ": Getting changesets from " + (MaxChangesetId + 1) + " to current ...", "info");
            // TFS 2010 doesn't like when we ask for history past its last changeset.
            if (MaxChangesetId == Tfs.GetLatestChangeset(this).Summary.ChangesetId)
                return Enumerable.Empty<ITfsChangeset>();
            return Tfs.GetChangesets(TfsRepositoryPath, MaxChangesetId + 1, this);
        }

        public ITfsChangeset GetChangeset(long changesetId)
        {
            return Tfs.GetChangeset((int) changesetId, this);
        }

        public void UpdateRef(string commitHash, long changesetId)
        {
            MaxCommitHash = commitHash;
            MaxChangesetId = changesetId;
            Repository.CommandNoisy("update-ref", "-m", "C" + MaxChangesetId, RemoteRef, MaxCommitHash);
            if (Autotag)
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
                try
                {
                    Repository.CommandNoisy("gc", "--auto");
                }
                catch(Exception e)
                {
                    Trace.WriteLine(e);
                    stdout.WriteLine("Warning: `git gc` failed! Try running it after git-tfs is finished.");
                }
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
                                                      procIn.WriteLine(GitTfsConstants.TfsCommitInfoFormat, TfsUrl,
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

        public void Unshelve(string shelvesetOwner, string shelvesetName, string destinationBranch)
        {
            var destinationRef = "refs/heads/" + destinationBranch;
            if (File.Exists(Path.Combine(Repository.GitDir, destinationRef)))
                throw new GitTfsException("ERROR: Destination branch (" + destinationBranch + ") already exists!");
            var shelvesetChangeset = Tfs.GetShelvesetData(this, shelvesetOwner, shelvesetName);
            Apply(shelvesetChangeset, destinationRef);
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
            PendChangesToWorkspace(head, parentChangeset.GitCommit, workspace);
            workspace.Shelve(shelvesetName, evaluateCheckinPolicies, () => Repository.GetCommitMessage(head, parentChangeset.GitCommit));
        }

        public long CheckinTool(string head, TfsChangesetInfo parentChangeset)
        {
            var changeset = 0L;
            Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset,
                              workspace => changeset = CheckinTool(head, parentChangeset, workspace));
            return changeset;
        }

        private long CheckinTool(string head, TfsChangesetInfo parentChangeset, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parentChangeset.GitCommit, workspace);
            return workspace.CheckinTool(() => Repository.GetCommitMessage(head, parentChangeset.GitCommit));
        }

        private void PendChangesToWorkspace(string head, string parent, ITfsWorkspace workspace)
        {
            foreach (var change in Repository.GetChangedFiles(parent, head))
            {
                change.Apply(workspace);
            }
        }

        public long Checkin(string head, TfsChangesetInfo parentChangeset)
        {
            var changeset = 0L;
            Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset,
                              workspace => changeset = Checkin(head, parentChangeset.GitCommit, workspace));
            return changeset;
        }

        public long Checkin(string head, string parent, TfsChangesetInfo parentChangeset)
        {
            var changeset = 0L;
            Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset,
                              workspace => changeset = Checkin(head, parent, workspace));
            return changeset;
        }

        private long Checkin(string head, string parent, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parent, workspace);
            return workspace.Checkin();
        }
    }
}
