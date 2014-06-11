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
        private static readonly Regex isInDotGit = new Regex("(?:^|/)\\.git(?:/|$)", RegexOptions.Compiled);

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

        private TfsChangesetInfo GetTfsChangesetById(long id)
        {
            return Repository.GetTfsChangesetById(RemoteRef, id);
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
                if (tfsPath.Length > TfsRepositoryPath.Length && tfsPath[TfsRepositoryPath.Length] != '/')
                    return null;
                tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);
            }
            else
            {
                //look through the subtrees
                var p = this.globals.Repository.GetSubtrees(this)
                            .Where(x => x.IsSubtree)
                            .FirstOrDefault(x => tfsPath.StartsWith(x.TfsRepositoryPath, StringComparison.InvariantCultureIgnoreCase)
                                && (tfsPath.Length == x.TfsRepositoryPath.Length || tfsPath[x.TfsRepositoryPath.Length] == '/'));
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
            return FetchWithMerge(mergeChangesetId, stopOnFailMergeCommit, -1, parentCommitsHashes);
        }

        public IFetchResult FetchWithMerge(long mergeChangesetId, bool stopOnFailMergeCommit = false, int lastChangesetIdToFetch = -1, params string[] parentCommitsHashes)
        {
            var fetchResult = new FetchResult { IsSuccess = true };
            var fetchedChangesets = FetchChangesets();
            int count = 0;
            var objects = new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var changeset in fetchedChangesets)
            {
                count++;
                var log = Apply(MaxCommitHash, changeset);
                if (lastChangesetIdToFetch > 0 && changeset.Summary.ChangesetId > lastChangesetIdToFetch)
                {
                    fetchResult.NewChangesetCount = count;
                    fetchResult.LastFetchedChangesetId = MaxChangesetId;
                    return fetchResult;
                }
                if (changeset.IsMergeChangeset && !ProcessMergeChangeset(changeset, stopOnFailMergeCommit, log))
                {
                    fetchResult.IsSuccess = false;
                    fetchResult.NewChangesetCount = count;
                    fetchResult.LastFetchedChangesetId = MaxChangesetId;
                    return fetchResult;
                }
                if (changeset.Summary.ChangesetId == mergeChangesetId)
                {
                    foreach (var parent in parentCommitsHashes)
                    {
                        log.CommitParents.Add(parent);
                    }
                }
                var commitSha = ProcessChangeset(changeset, log);
                // set commit sha for added git objects
                foreach (var commit in objects)
                {
                    if (commit.Value.Commit == null)
                        commit.Value.Commit = commitSha;
                }
                DoGcIfNeeded();
            }
            fetchResult.NewChangesetCount = count;
            return fetchResult;
        }
        private bool ProcessMergeChangeset(ITfsChangeset changeset, bool stopOnFailMergeCommit, ref string parentCommit)
        {
            if (!Tfs.CanGetBranchInformation)
            {
                stdout.WriteLine("info: this changeset " + changeset.Summary.ChangesetId +
                                 " is a merge changeset. But was not treated as is because this version of TFS can't manage branches...");
                return true;
            }
            else if (Repository.GetConfig(GitTfsConstants.IgnoreBranches) != true.ToString())
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
                }
                ProcessChangeset(changeset, log);
                DoGcIfNeeded();
            }
            fetchResult.NewChangesetCount = count;
            return fetchResult;
        }

        private bool ProcessMergeChangeset(ITfsChangeset changeset, bool stopOnFailMergeCommit, LogEntry log)
        {
            if (!Tfs.CanGetBranchInformation)
            {
                stdout.WriteLine("info: this changeset " + changeset.Summary.ChangesetId +
                                 " is a merge changeset. But was not treated as is because this version of TFS can't manage branches...");
            }
            else if (Repository.GetConfig(GitTfsConstants.IgnoreBranches) != true.ToString())
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
                        return false;

                    stdout.WriteLine("warning: this changeset " + changeset.Summary.ChangesetId +
                                     " is a merge changeset. But git-tfs failed to find and fetch the parent changeset "
                                     + parentChangesetId + ". Parent changeset will be ignored...");
                }
            }
            else
            {
                stdout.WriteLine("info: this changeset " + changeset.Summary.ChangesetId +
                                 " is a merge changeset. But was not treated as is because of your git setting...");
            }
            return true;
        }

        private string ProcessChangeset(ITfsChangeset changeset, LogEntry log)
        {
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
            if (changeset.Summary.Workitems.Any())
            {
                string workitemNote = "Workitems:\n";
                foreach (var workitem in changeset.Summary.Workitems)
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
            return commitSha;
        }

        private string FindMergedRemoteAndFetch(int parentChangesetId, bool stopOnFailMergeCommit)
        {
            var tfsRemotes = FindTfsRemoteOfChangeset(Tfs.GetChangeset(parentChangesetId), stopOnFailMergeCommit);
            foreach (var tfsRemote in tfsRemotes.Where(r=>string.Compare(r.TfsRepositoryPath, this.TfsRepositoryPath, StringComparison.InvariantCultureIgnoreCase) != 0))
            {
                stdout.WriteLine("\tFetching from dependent TFS remote '{0}'...", tfsRemote.Id);
                var fetchResult = ((GitTfsRemote)tfsRemote).FetchWithMerge(-1, stopOnFailMergeCommit, parentChangesetId);
            }
            return Repository.FindCommitHashByChangesetId(parentChangesetId);
        }

        private IEnumerable<IGitTfsRemote> FindTfsRemoteOfChangeset(IChangeset parentChangeset, bool stopOnFailMergeCommit)
        {
            //I think you want something that uses GetPathInGitRepo and ShouldSkip. See TfsChangeset.Apply.
            //Don't know if there is a way to extract remote tfs repository path from changeset datas! Should be better!!!
            var remote = Repository.ReadAllTfsRemotes().FirstOrDefault(r => parentChangeset.Changes.Any(c => r.GetPathInGitRepo(c.Item.ServerItem) != null));
            if (remote != null)
                return new List<IGitTfsRemote>() { remote };
            var tfsPath = parentChangeset.Changes.First().Item.ServerItem;
            tfsPath = tfsPath.EndsWith("/") ? tfsPath : tfsPath + "/";
            var tfsBranch = Tfs.GetBranches(true).SingleOrDefault(b => tfsPath.StartsWith(b.Path.EndsWith("/") ? b.Path : b.Path + "/"));

            if (tfsBranch == null)
            {
                stdout.WriteLine("error: branch not found. Verify that all the folders have been converted to branches (or something else :().");
                return new List<IGitTfsRemote>();
            }

            if (tfsBranch.IsRoot)
            {
                remote = InitTfsBranch(this.remoteOptions, tfsBranch.Path);
                return new List<IGitTfsRemote>() {remote};
            }

            var branchesDatas = Tfs.GetRootChangesetForBranch(tfsBranch.Path);

            foreach (var branch in branchesDatas)
            {
                var rootChangesetId = branch.RootChangeset;
                var sha1RootCommit = Repository.FindCommitHashByChangesetId(rootChangesetId);
                if (string.IsNullOrWhiteSpace(sha1RootCommit))
                {
                    sha1RootCommit = FindMergedRemoteAndFetch(rootChangesetId, stopOnFailMergeCommit);
                    if (string.IsNullOrWhiteSpace(sha1RootCommit))
                    {
                        stdout.WriteLine("error: root commit not found corresponding to changeset " + rootChangesetId);
                        return new List<IGitTfsRemote>();
                    }
                }
                remote = InitBranch(this.remoteOptions, tfsBranch.Path, sha1RootCommit);

                if (branch.IsRenamedBranch)
                    remote.Fetch();
            }

            return new List<IGitTfsRemote>(){remote};
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
                Repository.GarbageCollect(true, "Try running it after git-tfs is finished.");
            }
        }

        private LogEntry Apply(string parent, ITfsChangeset changeset, IDictionary<string, GitObject> entries)
        {
            LogEntry result = null;
            WithWorkspace(changeset.Summary, workspace =>
            {
                var treeBuilder = workspace.Remote.Repository.GetTreeBuilder(parent);
                result = changeset.Apply(parent, treeBuilder, workspace, entries);
                result.Tree = treeBuilder.GetTree();
            });
            if (!String.IsNullOrEmpty(parent)) result.CommitParents.Add(parent);
            return result;
        }

        private LogEntry Apply(string parent, ITfsChangeset changeset)
        {
            IDictionary<string, GitObject> entries = new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
            return Apply(parent, changeset, entries);
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
            logEntry.Log = BuildCommitMessage(logEntry.Log, logEntry.ChangesetId);
            return Repository.Commit(logEntry).Sha;
        }

        private string BuildCommitMessage(string tfsCheckinComment, long changesetId)
        {
            var builder = new StringWriter();
            builder.WriteLine(tfsCheckinComment);
            builder.WriteLine(GitTfsConstants.TfsCommitInfoFormat,
                TfsUrl, TfsRepositoryPath, changesetId);
            return builder.ToString();
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

        private string ExtractGitBranchNameFromTfsRepositoryPath(string tfsRepositoryPath)
        {
            var gitBranchName = Repository.AssertValidBranchName(tfsRepositoryPath.ToGitBranchNameFromTfsRepositoryPath());
            stdout.WriteLine("The name of the local branch will be : " + gitBranchName);
            return gitBranchName;
        }

        public IGitTfsRemote InitBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, string sha1RootCommit, string gitBranchNameExpected = null)
        {
            return InitTfsBranch(remoteOptions, tfsRepositoryPath, sha1RootCommit, gitBranchNameExpected);
        }

        private IGitTfsRemote InitTfsBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, string sha1RootCommit = null, string gitBranchNameExpected = null)
        {
            Trace.WriteLine("Begin process of creating branch for remote :" + tfsRepositoryPath);
            // TFS string representations of repository paths do not end in trailing slashes
            tfsRepositoryPath = (tfsRepositoryPath ?? string.Empty).TrimEnd('/');

            string gitBranchName = ExtractGitBranchNameFromTfsRepositoryPath(
                string.IsNullOrWhiteSpace(gitBranchNameExpected) ? tfsRepositoryPath : gitBranchNameExpected);
            if (string.IsNullOrWhiteSpace(gitBranchName))
                throw new GitTfsException("error: The Git branch name '" + gitBranchName + "' is not valid...\n");
            Trace.WriteLine("Git local branch will be :" + gitBranchName);

            if (Repository.HasRemote(gitBranchName))
            {
                Trace.WriteLine("Remote already exist");
                var remote = Repository.ReadTfsRemote(gitBranchName);
                if (remote.TfsUrl != TfsUrl)
                    Trace.WriteLine("warning: Url is different");
                if (remote.TfsRepositoryPath != tfsRepositoryPath)
                    Trace.WriteLine("warning: TFS repository path is different");
                return remote;
            }

            Trace.WriteLine("Try creating remote...");
            var tfsRemote = Repository.CreateTfsRemote(new RemoteInfo
            {
                Id = gitBranchName,
                Url = TfsUrl,
                Repository = tfsRepositoryPath,
                RemoteOptions = remoteOptions
            }, string.Empty);
            if (sha1RootCommit != null)
            {
                if (!Repository.CreateBranch(tfsRemote.RemoteRef, sha1RootCommit))
                    throw new GitTfsException("error: Fail to create remote branch ref file!");
            }
            Trace.WriteLine("Remote created!");
            return tfsRemote;
        }
    }
}
