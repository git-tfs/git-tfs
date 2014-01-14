using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;

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
        private bool isTfsAuthenticated;
        public RemoteInfo RemoteInfo { get; private set; }

        public GitTfsRemote(RemoteInfo info, IGitRepository repository, RemoteOptions remoteOptions, Globals globals, ITfsHelper tfsHelper, TextWriter stdout)
        {
            this.remoteOptions = remoteOptions;
            this.globals = globals;
            this.stdout = stdout;
            Tfs = tfsHelper;
            Repository = repository;

            RemoteInfo = info;
            Id = info.Id;
            TfsUrl = info.Url;
            TfsRepositoryPath = info.Repository;
            TfsUsername = info.Username;
            TfsPassword = info.Password;
            Aliases = (info.Aliases ?? Enumerable.Empty<string>()).ToArray();
            IgnoreRegexExpression = info.IgnoreRegex;
            IgnoreExceptRegexExpression = info.IgnoreExceptRegex;

            Autotag = info.Autotag;

            this.IsSubtree = CheckSubtree();
        }

        private bool CheckSubtree()
        {
            var m = GitTfsConstants.RemoteSubtreeRegex.Match(this.Id);
            if (m.Success)
            {
                this.OwningRemoteId = m.Groups["owner"].Value;
                this.Prefix = m.Groups["prefix"].Value;
                return true;
            }

            return false;
        }

        public void EnsureTfsAuthenticated()
        {
            if (isTfsAuthenticated)
                return;
            Tfs.EnsureAuthenticated();
            isTfsAuthenticated = true;
        }

        public bool IsDerived
        {
            get { return false; }
        }

        public bool IsSubtree { get; private set; }

        public bool IsSubtreeOwner
        {
            get
            {
                return TfsRepositoryPath == null;
            }
        }

        public string Id { get; set; }

        public string TfsUrl
        {
            get { return Tfs.Url; }
            set { Tfs.Url = value; }
        }

        private string[] Aliases { get; set; }

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

        /// <summary>
        /// Gets the TFS server-side paths of all subtrees of this remote.
        /// Valid if the remote has subtrees, which occurs when <see cref="TfsRepositoryPath"/> is null.
        /// </summary>
        public string[] TfsSubtreePaths 
        { 
            get
            {
                if (tfsSubtreePaths == null)
                    tfsSubtreePaths = Repository.GetSubtrees(this).Select(x => x.TfsRepositoryPath).ToArray();

                return tfsSubtreePaths;
            } 
        }
        private string[] tfsSubtreePaths = null;

        public string IgnoreRegexExpression { get; set; }
        public string IgnoreExceptRegexExpression { get; set; }
        public IGitRepository Repository { get; set; }
        public ITfsHelper Tfs { get; set; }

        public string OwningRemoteId { get; private set; }

        public string Prefix { get; private set; }
        public bool ExportMetadatas { get; set; }
        public Dictionary<string, string> ExportWorkitemsMapping { get; set; }

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

        private TfsChangesetInfo GetTfsChangesetById(int id)
        {
            return Repository.FilterParentTfsCommits(RemoteRef, false, c => c.ChangesetId == id).FirstOrDefault();
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
                var dir = Repository.GetConfig(GitTfsConstants.WorkspaceConfigKey);

                if (this.IsSubtree)
                {
                    if(dir != null)
                    {
                        return Path.Combine(dir, this.Prefix);
                    }

                    //find the relative path to the owning remote
                    return Ext.CombinePaths(globals.GitDir, "tfs", this.OwningRemoteId, "workspace", this.Prefix);
                }

                return dir ?? DefaultWorkingDirectory;
            }
        }

        private string DefaultWorkingDirectory
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

        public void CleanupWorkspaceDirectory()
        {
            try
            {
                if (Directory.Exists(WorkingDirectory))
                {
                    var allFiles = Directory.EnumerateFiles(WorkingDirectory, "*", SearchOption.AllDirectories);
                    foreach (var file in allFiles)
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);

                    Directory.Delete(WorkingDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("CleanupWorkspaceDirectory: " + ex.Message);
            }
        }

        public bool ShouldSkip(string path)
        {
            return IsInDotGit(path) || IsIgnored(path);
        }

        private bool IsIgnored(string path)
        {
            return Ignorance.IsIncluded(path);
        }

        private Bouncer _ignorance;
        private Bouncer Ignorance
        {
            get
            {
                if (_ignorance == null)
                {
                    _ignorance = new Bouncer();
                    _ignorance.Include(IgnoreRegexExpression);
                    _ignorance.Include(remoteOptions.IgnoreRegex);
                    _ignorance.Exclude(IgnoreExceptRegexExpression);
                    _ignorance.Exclude(remoteOptions.ExceptRegex);
                }
                return _ignorance;
            }
        }

        private bool IsInDotGit(string path)
        {
            return isInDotGit.IsMatch(path);
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            if (tfsPath == null) return null;

            if (!IsSubtreeOwner)
            {
                if (!tfsPath.StartsWith(TfsRepositoryPath, StringComparison.InvariantCultureIgnoreCase)) return null;
                tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);

            }
            else
            {
                //look through the subtrees
                var p = this.globals.Repository.GetSubtrees(this)
                            .Where(x => x.IsSubtree)
                            .FirstOrDefault(x => tfsPath.StartsWith(x.TfsRepositoryPath, StringComparison.InvariantCultureIgnoreCase));
                if (p == null) return null;

                tfsPath = p.GetPathInGitRepo(tfsPath);

                //we must prepend the prefix in order to get the correct directory
                if (tfsPath.StartsWith("/"))
                    tfsPath = p.Prefix + tfsPath;
                else
                    tfsPath = p.Prefix + "/" + tfsPath;
            }
            
            while (tfsPath.StartsWith("/"))
                tfsPath = tfsPath.Substring(1);
            return tfsPath;
        }

        public class FetchResult : IFetchResult
        {
            public bool IsSuccess { get; set; }
            public long LastFetchedChangesetId { get; set; }
            public int NewChangesetCount { get; set; }
            public string ParentBranchTfsPath { get; set; }
        }

        public IFetchResult Fetch(bool stopOnFailMergeCommit = false)
        {
            return FetchWithMerge(-1, stopOnFailMergeCommit);
        }

        public IFetchResult FetchWithMerge(long mergeChangesetId, bool stopOnFailMergeCommit = false, params string[] parentCommitsHashes)
        {
            var fetchResult = new FetchResult{IsSuccess = true};
            var fetchedChangesets = FetchChangesets().ToList();
            fetchResult.NewChangesetCount = fetchedChangesets.Count;
            foreach (var changeset in fetchedChangesets)
            {
                var log = Apply(MaxCommitHash, changeset);
                if (changeset.IsMergeChangeset)
                {
                    var parentChangesetId = Tfs.FindMergeChangesetParent(TfsRepositoryPath, changeset.Summary.ChangesetId, this);
                    var shaParent = Repository.FindCommitHashByChangesetId(parentChangesetId);
                    if (shaParent == null)
                        shaParent = FindMergedRemoteAndFetch(parentChangesetId, stopOnFailMergeCommit);
                    if (shaParent != null)
                    {
                        log.CommitParents.Add(shaParent);
                    }
                    else
                    {
                        if (stopOnFailMergeCommit)
                        {
                            fetchResult.IsSuccess = false;
                            fetchResult.LastFetchedChangesetId = MaxChangesetId;
                            return fetchResult;
                        }
//TODO : Manage case where there is not yet a git commit for the parent changset!!!!!
                        stdout.WriteLine("warning: this changeset " + changeset.Summary.ChangesetId +
                        " is a merge changeset. But it can't have been managed accordingly because one of the parent changeset "
                        + parentChangesetId + " is not present in the repository! If you want to do it, fetch the branch containing this changeset before retrying...");
                    }
                }
                if (changeset.Summary.ChangesetId == mergeChangesetId)
                {
                    foreach (var parent in parentCommitsHashes)
                    {
                        log.CommitParents.Add(parent);
                    }
                }

                if (ExportMetadatas)
                {
                    if (changeset.Summary.Workitems.Any())
                    {
                        log.Log += "\nwork-items: " + string.Join(", ", changeset.Summary.Workitems.Select(wi => "#" + wi.Id)); ;
                    }

                    if (ExportWorkitemsMapping.Count != 0)
                    {
                        foreach (var mapping in ExportWorkitemsMapping)
                        {
                            log.Log = log.Log.Replace("#" + mapping.Key, "#" + mapping.Value);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(changeset.Summary.PolicyOverrideComment))
                        log.Log += "\n" + GitTfsConstants.GitTfsPolicyOverrideCommentPrefix + changeset.Summary.PolicyOverrideComment;

                    if (!string.IsNullOrWhiteSpace(changeset.Summary.CodeReviewer))
                        log.Log += "\n" + GitTfsConstants.GitTfsCodeReviewerPrefix + changeset.Summary.CodeReviewer;

                    if (!string.IsNullOrWhiteSpace(changeset.Summary.SecurityReviewer))
                        log.Log += "\n" + GitTfsConstants.GitTfsSecurityReviewerPrefix + changeset.Summary.SecurityReviewer;

                    if (!string.IsNullOrWhiteSpace(changeset.Summary.PerformanceReviewer))
                        log.Log += "\n" + GitTfsConstants.GitTfsPerformanceReviewerPrefix + changeset.Summary.PerformanceReviewer;
                }

                var commitSha = Commit(log);
                UpdateTfsHead(commitSha, changeset.Summary.ChangesetId);
                StringBuilder metadatas = new StringBuilder();
                if(changeset.Summary.Workitems.Any())
                {
                    string workitemNote = "Workitems:\n";
                    foreach(var workitem in changeset.Summary.Workitems)
                    {
                        var workitemId = workitem.Id.ToString();
                        var workitemUrl = workitem.Url;
                        if (ExportMetadatas && ExportWorkitemsMapping.Count != 0)
                        {
                            if (ExportWorkitemsMapping.ContainsKey(workitemId))
                            {
                                var oldWorkitemId = workitemId;
                                workitemId = ExportWorkitemsMapping[workitemId];
                                workitemUrl = workitemUrl.Replace(oldWorkitemId, workitemId);
                            }
                        }
                        workitemNote += String.Format("[{0}] {1}\n    {2}\n", workitemId, workitem.Title, workitemUrl);
                    }
                    metadatas.Append(workitemNote);
                }

                if (!string.IsNullOrWhiteSpace(changeset.Summary.PolicyOverrideComment))
                    metadatas.Append("\nPolicy Override Comment:" + changeset.Summary.PolicyOverrideComment);

                if (!string.IsNullOrWhiteSpace(changeset.Summary.CodeReviewer))
                    metadatas.Append("\nCode Reviewer:" + changeset.Summary.CodeReviewer);

                if (!string.IsNullOrWhiteSpace(changeset.Summary.SecurityReviewer))
                    metadatas.Append("\nSecurity Reviewer:" + changeset.Summary.SecurityReviewer);

                if (!string.IsNullOrWhiteSpace(changeset.Summary.PerformanceReviewer))
                    metadatas.Append("\nPerformance Reviewer:" + changeset.Summary.PerformanceReviewer);
                if (metadatas.Length != 0)
                    Repository.CreateNote(commitSha, metadatas.ToString(), log.AuthorName, log.AuthorEmail, log.Date);
                DoGcIfNeeded();
            }
            return fetchResult;
        }

        private string FindMergedRemoteAndFetch(int parentChangesetId, bool stopOnFailMergeCommit)
        {
            var tfsRemotes = FindTfsRemoteOfChangeset(Tfs.GetChangeset(parentChangesetId));
            foreach (var tfsRemote in tfsRemotes.Where(r=>string.Compare(r.TfsRepositoryPath, this.TfsRepositoryPath, StringComparison.InvariantCultureIgnoreCase) != 0))
            {
                var fetchResult = tfsRemote.Fetch(stopOnFailMergeCommit);
            }
            return Repository.FindCommitHashByChangesetId(parentChangesetId);
        }

        private IEnumerable<IGitTfsRemote> FindTfsRemoteOfChangeset(IChangeset changeset)
        {
            //I think you want something that uses GetPathInGitRepo and ShouldSkip. See TfsChangeset.Apply.
            //Don't know if there is a way to extract remote tfs repository path from changeset datas! Should be better!!!
            return Repository.ReadAllTfsRemotes().Where(r => changeset.Changes.Any(c => r.GetPathInGitRepo(c.Item.ServerItem) != null));
        }

        private string CommitChangeset(ITfsChangeset changeset, string parent)
        {
            var log = Apply(parent, changeset);
            return Commit(log);
        }

        public void QuickFetch()
        {
            var changeset = GetLatestChangeset();
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
            UpdateTfsHead(Commit(log), changeset.Summary.ChangesetId);
            DoGcIfNeeded();
        }

        private IEnumerable<ITfsChangeset> FetchChangesets()
        {
            Trace.WriteLine(RemoteRef + ": Getting changesets from " + (MaxChangesetId + 1) + " to current ...", "info");
            // TFS 2010 doesn't like when we ask for history past its last changeset.
            if (MaxChangesetId == GetLatestChangeset().Summary.ChangesetId)
                return Enumerable.Empty<ITfsChangeset>();
            
            if(!IsSubtreeOwner)
                return Tfs.GetChangesets(TfsRepositoryPath, MaxChangesetId + 1, this);

            return globals.Repository.GetSubtrees(this)
                .SelectMany(x => Tfs.GetChangesets(x.TfsRepositoryPath, this.MaxChangesetId + 1, x))
                .OrderBy(x => x.Summary.ChangesetId);
        }

        public ITfsChangeset GetChangeset(long changesetId)
        {
            return Tfs.GetChangeset((int)changesetId, this);
        }

        private ITfsChangeset GetLatestChangeset()
        {
            if (!string.IsNullOrEmpty(this.TfsRepositoryPath))
            {
                return Tfs.GetLatestChangeset(this);
            }
            else
            {
                var changesetId = globals.Repository.GetSubtrees(this).Select(x => Tfs.GetLatestChangeset(x)).Max(x => x.Summary.ChangesetId);
                return GetChangeset(changesetId);
            }
        }

        public void UpdateTfsHead(string commitHash, long changesetId)
        {
            MaxCommitHash = commitHash;
            MaxChangesetId = changesetId;
            Repository.UpdateRef(RemoteRef, MaxCommitHash, "C" + MaxChangesetId);
            if (Autotag)
                Repository.UpdateRef(TagPrefix + "C" + MaxChangesetId, MaxCommitHash);
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
            if (--globals.GcCountdown < 0)
            {
                globals.GcCountdown = globals.GcPeriod;
                try
                {
                    Repository.CommandNoisy("gc", "--auto");
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                    stdout.WriteLine("Warning: `git gc` failed! Try running it after git-tfs is finished.");
                }
            }
        }

        private void AssertTemporaryIndexClean(string treeish)
        {
            if (string.IsNullOrEmpty(treeish))
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

        private LogEntry Apply(string parent, ITfsChangeset changeset)
        {
            LogEntry result = null;
            WithWorkspace(changeset.Summary, workspace =>
            {
                var treeBuilder = workspace.Remote.Repository.GetTreeBuilder(parent);
                result = changeset.Apply(parent, treeBuilder, workspace);
                result.Tree = treeBuilder.GetTree();
            });
            if (!String.IsNullOrEmpty(parent)) result.CommitParents.Add(parent);
            return result;
        }

        private LogEntry CopyTree(string lastCommit, ITfsChangeset changeset)
        {
            LogEntry result = null;
            WithWorkspace(changeset.Summary, workspace => {
                var treeBuilder = workspace.Remote.Repository.GetTreeBuilder(null);
                result = changeset.CopyTree(treeBuilder, workspace);
                result.Tree = treeBuilder.GetTree();
            });
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

            //the remote to be associated with the commit might be a subtree, if it's null then it's not from a subtree.
            var remote = logEntry.Remote ?? this;
            Repository.CommandInputOutputPipe((procIn, procOut) =>
                                                  {
                                                      procIn.WriteLine(logEntry.Log);
                                                      procIn.WriteLine(GitTfsConstants.TfsCommitInfoFormat, remote.TfsUrl,
                                                                       remote.TfsRepositoryPath, logEntry.ChangesetId);
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
                                         }, new Dictionary<string, string> { { "GIT_INDEX_FILE", IndexFile } });
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
            foreach (var key in desiredEnvironment.Keys)
            {
                oldEnvironment[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, desiredEnvironment[key]);
            }
        }

        public void Unshelve(string shelvesetOwner, string shelvesetName, string destinationBranch)
        {
            var destinationRef = GitRepository.ShortToLocalName(destinationBranch);
            if(Repository.HasRef(destinationRef))
                throw new GitTfsException("ERROR: Destination branch (" + destinationBranch + ") already exists!");

            var shelvesetChangeset = Tfs.GetShelvesetData(this, shelvesetOwner, shelvesetName);

            var parentId = shelvesetChangeset.BaseChangesetId;
            var ch = GetTfsChangesetById(parentId);
            if (ch == null)
                throw new GitTfsException("ERROR: Parent changeset C" + parentId  + " not found."
                                         +" Try fetching the latest changes from TFS");

            var commit = CommitChangeset(shelvesetChangeset, ch.GitCommit);
            Repository.UpdateRef(destinationRef, commit, "Shelveset " + shelvesetName + " from " + shelvesetOwner);
        }

        public void Shelve(string shelvesetName, string head, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies)
        {
            WithWorkspace(parentChangeset, workspace => Shelve(shelvesetName, head, parentChangeset, evaluateCheckinPolicies, workspace));
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
            WithWorkspace(parentChangeset, workspace => changeset = CheckinTool(head, parentChangeset, workspace));
            return changeset;
        }

        private long CheckinTool(string head, TfsChangesetInfo parentChangeset, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parentChangeset.GitCommit, workspace);
            return workspace.CheckinTool(() => Repository.GetCommitMessage(head, parentChangeset.GitCommit));
        }

        private void PendChangesToWorkspace(string head, string parent, ITfsWorkspaceModifier workspace)
        {
            using (var tidyWorkspace = new DirectoryTidier(workspace, GetLatestChangeset().GetFullTree()))
            {
                foreach (var change in Repository.GetChangedFiles(parent, head))
                {
                    change.Apply(tidyWorkspace);
                }
            }
        }

        public long Checkin(string head, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null)
        {
            var changeset = 0L;
            WithWorkspace(parentChangeset, workspace => changeset = Checkin(head, parentChangeset.GitCommit, workspace, options, sourceTfsPath));
            return changeset;
        }

        public long Checkin(string head, string parent, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null)
        {
            var changeset = 0L;
            WithWorkspace(parentChangeset, workspace => changeset = Checkin(head, parent, workspace, options, sourceTfsPath));
            return changeset;
        }

        private void WithWorkspace(TfsChangesetInfo parentChangeset, Action<ITfsWorkspace> action)
        {
            //are there any subtrees?
            var subtrees = globals.Repository.GetSubtrees(this);
            if (subtrees.Any())
            {
                Tfs.WithWorkspace(WorkingDirectory, this, subtrees.Select(x => new Tuple<string, string>(x.TfsRepositoryPath, x.Prefix)), parentChangeset, action);
            }
            else
            {
                Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset, action);
            }
        }

        private long Checkin(string head, string parent, ITfsWorkspace workspace, CheckinOptions options, string sourceTfsPath)
        {
            PendChangesToWorkspace(head, parent, workspace);
            if (!string.IsNullOrWhiteSpace(sourceTfsPath))
                workspace.Merge(sourceTfsPath, TfsRepositoryPath);
            return workspace.Checkin(options);
        }

        public bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath)
        {
            if(!MatchesTfsUrl(tfsUrl))
                return false;

            if(TfsRepositoryPath == null)
                return tfsRepositoryPath == null;
            
            return TfsRepositoryPath.Equals(tfsRepositoryPath, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesTfsUrl(string tfsUrl)
        {
            return TfsUrl.Equals(tfsUrl, StringComparison.OrdinalIgnoreCase) || Aliases.Contains(tfsUrl, StringComparison.OrdinalIgnoreCase);
        }
    }
}
