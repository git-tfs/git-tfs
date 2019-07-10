using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using GitTfs.Commands;
using GitTfs.Core.TfsInterop;
using GitTfs.Util;

namespace GitTfs.Core
{
    public class GitTfsRemote : IGitTfsRemote
    {
        private static readonly Regex isInDotGit = new Regex("(?:^|/)\\.git(?:/|$)", RegexOptions.Compiled);

        private readonly Globals _globals;
        private readonly RemoteOptions _remoteOptions;
        private readonly ConfigProperties _properties;
        private int? firstChangesetId;
        private int? maxChangesetId;
        private string maxCommitHash;
        private bool isTfsAuthenticated;
        public RemoteInfo RemoteInfo { get; private set; }

        private static int remoteCounter = 0;

        /// <summary>
        ///    Holds the subdirectory path for this remote so that each remote doesn't conflict with each other and doesn't cause the worskspaces to get created and destroyed over and over
        /// </summary>
        private readonly string _workingSubDirectory;

        public GitTfsRemote(RemoteInfo info, IGitRepository repository, RemoteOptions remoteOptions, Globals globals,
            ITfsHelper tfsHelper, ConfigProperties properties)
        {
            _remoteOptions = remoteOptions;
            _globals = globals;
            _properties = properties;
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

            IsSubtree = CheckSubtree();

            var value = Interlocked.Increment(ref remoteCounter);
            _workingSubDirectory =  value.ToString("X4");
        }

        private bool CheckSubtree()
        {
            var m = GitTfsConstants.RemoteSubtreeRegex.Match(Id);
            if (m.Success)
            {
                OwningRemoteId = m.Groups["owner"].Value;
                Prefix = m.Groups["prefix"].Value;
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

        public int? GetFirstChangeset()
        {
            return firstChangesetId;
        }

        public void SetFirstChangeset(int? changesetId)
        {
            Trace.WriteLine($"Set first changeset in branch to C{changesetId}");
            firstChangesetId = changesetId;
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
        public Dictionary<string, IExportWorkItem> ExportWorkitemsMapping { get; set; }

        public int MaxChangesetId
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
                    //Manage the special case where a .gitignore has been commited
                    try
                    {
                        var gitCommit = Repository.GetCommit(RemoteRef);
                        if (gitCommit != null)
                        {
                            MaxCommitHash = gitCommit.Sha;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private const string WorkspaceDirectory = "~w";
        private string WorkingDirectory
        {
            get
            {
                var dir = Repository.GetConfig(GitTfsConstants.WorkspaceConfigKey);

                if (IsSubtree)
                {
                    if (dir != null)
                    {
                        return Path.Combine(dir, Prefix);
                    }

                    //find the relative path to the owning remote
                    return Ext.CombinePaths(_globals.GitDir, WorkspaceDirectory, OwningRemoteId, Prefix);
                }

                return dir ?? DefaultWorkingDirectory;
            }
        }

        private string DefaultWorkingDirectory
        {
            get
            {
                return Path.Combine(_globals.GitDir, WorkspaceDirectory, _workingSubDirectory);
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
            var isIgnored = Ignorance.IsIncluded(path) || Repository.IsPathIgnored(path);
            if (isIgnored)
            {
                Trace.TraceWarning("The file '{0}' is ignored due to `.gitignore` or ignore regex defined.", path);
            }

            return isIgnored;
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
                    _ignorance.Include(_remoteOptions.IgnoreRegex);
                    _ignorance.Exclude(IgnoreExceptRegexExpression);
                    _ignorance.Exclude(_remoteOptions.ExceptRegex);
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
                if (TfsRepositoryPath == GitTfsConstants.TfsRoot)
                {
                    tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);
                }
                else
                {
                    if (tfsPath.Length > TfsRepositoryPath.Length && tfsPath[TfsRepositoryPath.Length] != '/')
                        return null;
                    tfsPath = tfsPath.Substring(TfsRepositoryPath.Length);
                }
            }
            else
            {
                //look through the subtrees
                var p = _globals.Repository.GetSubtrees(this)
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
            public int LastFetchedChangesetId { get; set; }
            public int NewChangesetCount { get; set; }
            public string ParentBranchTfsPath { get; set; }
            public bool IsProcessingRenameChangeset { get; set; }
            public string LastParentCommitBeforeRename { get; set; }
        }

        public IFetchResult Fetch(bool stopOnFailMergeCommit = false, int lastChangesetIdToFetch = -1, IRenameResult renameResult = null)
        {
            return FetchWithMerge(-1, stopOnFailMergeCommit, lastChangesetIdToFetch, renameResult);
        }

        public IFetchResult FetchWithMerge(int mergeChangesetId, bool stopOnFailMergeCommit = false, IRenameResult renameResult = null, params string[] parentCommitsHashes)
        {
            return FetchWithMerge(mergeChangesetId, stopOnFailMergeCommit, -1, renameResult, parentCommitsHashes);
        }

        public IFetchResult FetchWithMerge(int mergeChangesetId, bool stopOnFailMergeCommit = false, int lastChangesetIdToFetch = -1, IRenameResult renameResult = null, params string[] parentCommitsHashes)
        {
            var fetchResult = new FetchResult { IsSuccess = true, NewChangesetCount = 0 };
            var latestChangesetId = GetLatestChangesetId();
            if (lastChangesetIdToFetch != -1)
                latestChangesetId = Math.Min(latestChangesetId, lastChangesetIdToFetch);
            // TFS 2010 doesn't like when we ask for history past its last changeset.
            if (MaxChangesetId >= latestChangesetId)
                return fetchResult;

            bool fetchRetrievedChangesets;
            do
            {
                var fetchedChangesets = FetchChangesets(true, lastChangesetIdToFetch);

                var objects = BuildEntryDictionary();
                fetchRetrievedChangesets = false;
                foreach (var changeset in fetchedChangesets)
                {
                    fetchRetrievedChangesets = true;

                    fetchResult.NewChangesetCount++;
                    if (lastChangesetIdToFetch > 0 && changeset.Summary.ChangesetId > lastChangesetIdToFetch)
                        return fetchResult;
                    string parentCommitSha = null;
                    if (changeset.IsMergeChangeset && !ProcessMergeChangeset(changeset, stopOnFailMergeCommit, ref parentCommitSha))
                    {
                        fetchResult.NewChangesetCount--; // Merge wasn't successful - so don't count the changeset we found
                        fetchResult.IsSuccess = false;
                        return fetchResult;
                    }
                    var parentSha = (renameResult != null && renameResult.IsProcessingRenameChangeset) ? renameResult.LastParentCommitBeforeRename : MaxCommitHash;
                    var isFirstCommitInRepository = (parentSha == null);
                    var log = Apply(parentSha, changeset, objects);
                    if (changeset.IsRenameChangeset && !isFirstCommitInRepository)
                    {
                        if (renameResult == null || !renameResult.IsProcessingRenameChangeset)
                        {
                            fetchResult.IsProcessingRenameChangeset = true;
                            fetchResult.LastParentCommitBeforeRename = MaxCommitHash;
                            return fetchResult;
                        }
                        renameResult.IsProcessingRenameChangeset = false;
                        renameResult.LastParentCommitBeforeRename = null;
                    }
                    if (parentCommitSha != null)
                        log.CommitParents.Add(parentCommitSha);
                    if (changeset.Summary.ChangesetId == mergeChangesetId)
                    {
                        foreach (var parent in parentCommitsHashes)
                            log.CommitParents.Add(parent);
                    }
                    var commitSha = ProcessChangeset(changeset, log);
                    fetchResult.LastFetchedChangesetId = changeset.Summary.ChangesetId;
                    // set commit sha for added git objects
                    foreach (var commit in objects)
                    {
                        if (commit.Value.Commit == null)
                            commit.Value.Commit = commitSha;
                    }
                    DoGcIfNeeded();
                }
            } while (fetchRetrievedChangesets && latestChangesetId > fetchResult.LastFetchedChangesetId);
            return fetchResult;
        }


        private Dictionary<string, GitObject> BuildEntryDictionary()
        {
            return new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase);
        }

        private bool ProcessMergeChangeset(ITfsChangeset changeset, bool stopOnFailMergeCommit, ref string parentCommit)
        {
            if (!Tfs.CanGetBranchInformation)
            {
                Trace.TraceInformation("info: this changeset " + changeset.Summary.ChangesetId +
                                 " is a merge changeset. But was not treated as is because this version of TFS can't manage branches...");
            }
            else if (!IsIgnoringBranches())
            {
                var parentChangesetId = Tfs.FindMergeChangesetParent(TfsRepositoryPath, changeset.Summary.ChangesetId, this);
                if (parentChangesetId < 1)  // Handle missing merge parent info
                {
                    if (stopOnFailMergeCommit)
                    {
                        return false;
                    }
                    Trace.TraceInformation("warning: this changeset " + changeset.Summary.ChangesetId +
                                     " is a merge changeset. But git-tfs is unable to determine the parent changeset.");
                    return true;
                }
                var shaParent = Repository.FindCommitHashByChangesetId(parentChangesetId);
                if (shaParent == null)
                {
                    string omittedParentBranch;
                    shaParent = FindMergedRemoteAndFetch(parentChangesetId, stopOnFailMergeCommit, out omittedParentBranch);
                    changeset.OmittedParentBranch = omittedParentBranch;
                }
                if (shaParent != null)
                {
                    parentCommit = shaParent;
                }
                else
                {
                    if (stopOnFailMergeCommit)
                        return false;

                    Trace.TraceInformation("warning: this changeset " + changeset.Summary.ChangesetId +
                                     " is a merge changeset. But git-tfs failed to find and fetch the parent changeset "
                                     + parentChangesetId + ". Parent changeset will be ignored...");
                }
            }
            else
            {
                Trace.TraceInformation("info: this changeset " + changeset.Summary.ChangesetId +
                                 " is a merge changeset. But was not treated as is because of your git setting...");
                changeset.OmittedParentBranch = ";C" + changeset.Summary.ChangesetId;
            }
            return true;
        }

        public bool IsIgnoringBranches()
        {
            var value = Repository.GetConfig<string>(GitTfsConstants.IgnoreBranches, null);
            bool isIgnoringBranches;
            if (value != null && bool.TryParse(value, out isIgnoringBranches))
                return isIgnoringBranches;

            Trace.TraceInformation("warning: no value found for branch management setting '" + GitTfsConstants.IgnoreBranches +
                             "'...");
            var isIgnoringBranchesDetected = Repository.ReadAllTfsRemotes().Count() < 2;
            Trace.TraceInformation("=> Branch support " + (isIgnoringBranchesDetected ? "disabled!" : "enabled!"));
            if (isIgnoringBranchesDetected)
                Trace.TraceInformation("   if you want to enable branch support, use the command:" + Environment.NewLine
                    + "    git config --local " + GitTfsConstants.IgnoreBranches + " false");
            _globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, isIgnoringBranchesDetected);
            return isIgnoringBranchesDetected;
        }

        private string ProcessChangeset(ITfsChangeset changeset, LogEntry log)
        {
            if (ExportMetadatas)
            {
                if (changeset.Summary.Workitems.Any())
                {
                    var workItems = TranslateWorkItems(changeset.Summary.Workitems.Select(wi => new ExportWorkItem(wi)));
                    if (workItems != null)
                    {
                        log.Log += "\nWorkitems:";
                        foreach (var workItem in workItems)
                        {
                            log.Log += "\n#" + workItem.Id + " " + workItem.Title;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(changeset.Summary.PolicyOverrideComment))
                    log.Log += "\n" + GitTfsConstants.GitTfsPolicyOverrideCommentPrefix + " " + changeset.Summary.PolicyOverrideComment;

                foreach (var checkinNote in changeset.Summary.CheckinNotes)
                {
                    if (!string.IsNullOrWhiteSpace(checkinNote.Name) && !string.IsNullOrWhiteSpace(checkinNote.Value))
                        log.Log += "\n" + GitTfsConstants.GitTfsPrefix + "-" + CamelCaseToDelimitedStringConverter.Convert(checkinNote.Name, "-") + ": " + checkinNote.Value;
                }
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
                            workitemId = ExportWorkitemsMapping[workitemId].Id;
                            workitemUrl = workitemUrl.Replace(oldWorkitemId, workitemId);
                        }
                    }
                    workitemNote += string.Format("[{0}] {1}\n    {2}\n", workitemId, workitem.Title, workitemUrl);
                }
                metadatas.Append(workitemNote);
            }

            if (!string.IsNullOrWhiteSpace(changeset.Summary.PolicyOverrideComment))
                metadatas.Append("\nPolicy Override Comment: " + changeset.Summary.PolicyOverrideComment);

            foreach (var checkinNote in changeset.Summary.CheckinNotes)
            {
                if (!string.IsNullOrWhiteSpace(checkinNote.Name) && !string.IsNullOrWhiteSpace(checkinNote.Value))
                    metadatas.Append("\n" + checkinNote.Name + ": " + checkinNote.Value);
            }

            if (!string.IsNullOrWhiteSpace(changeset.OmittedParentBranch))
                metadatas.Append("\nOmitted parent branch: " + changeset.OmittedParentBranch);

            if (metadatas.Length != 0)
                Repository.CreateNote(commitSha, metadatas.ToString(), log.AuthorName, log.AuthorEmail, log.Date);
            return commitSha;
        }

        private IEnumerable<IExportWorkItem> TranslateWorkItems(IEnumerable<IExportWorkItem> workItemsOriginal)
        {
            if (ExportWorkitemsMapping.Count == 0)
                return workItemsOriginal;
            List<IExportWorkItem> workItemsTranslated = new List<IExportWorkItem>();
            if (workItemsOriginal == null)
                return workItemsTranslated;
            foreach (var oldWorkItemId in workItemsOriginal)
            {
                IExportWorkItem translatedWorkItem = null;
                if (oldWorkItemId != null && !ExportWorkitemsMapping.TryGetValue(oldWorkItemId.Id, out translatedWorkItem))
                    translatedWorkItem = oldWorkItemId;
                if (translatedWorkItem != null)
                    workItemsTranslated.Add(translatedWorkItem);
            }
            return workItemsTranslated;
        }

        private string FindRootRemoteAndFetch(int parentChangesetId, IRenameResult renameResult = null)
        {
            string omittedParentBranch;
            return FindRemoteAndFetch(parentChangesetId, false, false, renameResult, out omittedParentBranch);
        }

        private string FindMergedRemoteAndFetch(int parentChangesetId, bool stopOnFailMergeCommit, out string omittedParentBranch)
        {
            return FindRemoteAndFetch(parentChangesetId, false, true, null, out omittedParentBranch);
        }

        private string FindRemoteAndFetch(int parentChangesetId, bool stopOnFailMergeCommit, bool mergeChangeset, IRenameResult renameResult, out string omittedParentBranch)
        {
            var tfsRemote = FindOrInitTfsRemoteOfChangeset(parentChangesetId, mergeChangeset, renameResult, out omittedParentBranch);

            if (tfsRemote != null && string.Compare(tfsRemote.TfsRepositoryPath, TfsRepositoryPath, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                Trace.TraceInformation("\tFetching from dependent TFS remote '{0}'...", tfsRemote.Id);
                try
                {
                    var fetchResult = ((GitTfsRemote)tfsRemote).FetchWithMerge(-1, stopOnFailMergeCommit, parentChangesetId, renameResult);
                }
                finally
                {
                    Trace.WriteLine("Cleaning...");
                    tfsRemote.CleanupWorkspaceDirectory();

                    if (tfsRemote.Repository.IsBare)
                        tfsRemote.Repository.UpdateRef(GitRepository.ShortToLocalName(tfsRemote.Id), tfsRemote.MaxCommitHash);
                }
                return Repository.FindCommitHashByChangesetId(parentChangesetId);
            }
            return null;
        }

        private IGitTfsRemote FindOrInitTfsRemoteOfChangeset(int parentChangesetId, bool mergeChangeset, IRenameResult renameResult, out string omittedParentBranch)
        {
            omittedParentBranch = null;
            IGitTfsRemote tfsRemote;
            IChangeset parentChangeset = Tfs.GetChangeset(parentChangesetId);
            //I think you want something that uses GetPathInGitRepo and ShouldSkip. See TfsChangeset.Apply.
            //Don't know if there is a way to extract remote tfs repository path from changeset datas! Should be better!!!
            var remote = Repository.ReadAllTfsRemotes().FirstOrDefault(r => parentChangeset.Changes.Any(c => r.GetPathInGitRepo(c.Item.ServerItem) != null));
            if (remote != null)
                tfsRemote = remote;
            else
            {
                // If the changeset has created multiple folders, the expected branch folder will not always be the first
                // so we scan all the changes of type folder to try to detect the first one which is a branch.
                // In most cases it will change nothing: the first folder is the good one
                IBranchObject tfsBranch = null;
                string tfsPath = null;
                var allBranches = Tfs.GetBranches(true);
                foreach (var change in parentChangeset.Changes)
                {
                    tfsPath = change.Item.ServerItem;
                    tfsPath = tfsPath.EndsWith("/") ? tfsPath : tfsPath + "/";

                    tfsBranch = allBranches.SingleOrDefault(b => tfsPath.StartsWith(b.Path.EndsWith("/") ? b.Path : b.Path + "/"));
                    if (tfsBranch != null)
                    {
                        // we found a branch, we stop here
                        break;
                    }
                }
                var filterRegex = Repository.GetConfig(GitTfsConstants.IgnoreBranchesRegex);
                if (mergeChangeset && tfsBranch != null && !string.IsNullOrEmpty(filterRegex)
                    && Regex.IsMatch(tfsBranch.Path, filterRegex, RegexOptions.IgnoreCase))
                {
                    Trace.TraceInformation("warning: skip filtered branch for path " + tfsBranch.Path + " (regex:" + filterRegex + ")");
                    tfsRemote = null;
                    omittedParentBranch = tfsBranch.Path + ";C" + parentChangesetId;
                }
                else if (mergeChangeset && tfsBranch != null &&
                    string.Equals(Repository.GetConfig(GitTfsConstants.IgnoreNotInitBranches), true.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    Trace.TraceInformation("warning: skip not initialized branch for path " + tfsBranch.Path);
                    tfsRemote = null;
                    omittedParentBranch = tfsBranch.Path + ";C" + parentChangesetId;
                }
                else if (tfsBranch == null)
                {
                    Trace.TraceInformation("error: branch not found. Verify that all the folders have been converted to branches (or something else :().\n\tpath {0}", tfsPath);
                    tfsRemote = null;
                    omittedParentBranch = ";C" + parentChangesetId;
                }
                else
                {
                    tfsRemote = InitTfsRemoteOfChangeset(tfsBranch, parentChangeset.ChangesetId, renameResult);
                    if (tfsRemote == null)
                        omittedParentBranch = tfsBranch.Path + ";C" + parentChangesetId;
                }
            }
            return tfsRemote;
        }

        private IGitTfsRemote InitTfsRemoteOfChangeset(IBranchObject tfsBranch, int parentChangesetId, IRenameResult renameResult = null)
        {
            if (tfsBranch.IsRoot)
            {
                return InitTfsBranch(_remoteOptions, tfsBranch.Path);
            }

            var branchesDatas = Tfs.GetRootChangesetForBranch(tfsBranch.Path, parentChangesetId);

            IGitTfsRemote remote = null;
            foreach (var branch in branchesDatas)
            {
                var rootChangesetId = branch.SourceBranchChangesetId;
                remote = InitBranch(_remoteOptions, tfsBranch.Path, rootChangesetId, true);
                if (remote == null)
                {
                    Trace.TraceInformation("warning: root commit not found corresponding to changeset " + rootChangesetId);
                    Trace.TraceInformation("=> continuing anyway by creating a branch without parent...");
                    return InitTfsBranch(_remoteOptions, tfsBranch.Path);
                }

                if (branch.IsRenamedBranch)
                {
                    try
                    {
                        remote.Fetch(renameResult: renameResult);
                    }
                    finally
                    {
                        Trace.WriteLine("Cleaning...");
                        remote.CleanupWorkspaceDirectory();

                        if (remote.Repository.IsBare)
                            remote.Repository.UpdateRef(GitRepository.ShortToLocalName(remote.Id), remote.MaxCommitHash);
                    }
                }
            }

            return remote;
        }

        public void QuickFetch(int changesetId, bool ignoreRestricted, bool printRestrictionHint)
        {
            try
            {
                ITfsChangeset changeset;
                if (changesetId < 0)
                    changeset = GetLatestChangeset();
                else
                    changeset = Tfs.GetChangeset(changesetId, this);
                quickFetch(changeset);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Quick fetch failed: " + ex.Message);
                if (!IgnoreException(ex.Message, ignoreRestricted, printRestrictionHint))
                    throw;
            }
        }

        private void quickFetch(ITfsChangeset changeset)
        {
            var log = CopyTree(MaxCommitHash, changeset);
            UpdateTfsHead(Commit(log), changeset.Summary.ChangesetId);
            DoGcIfNeeded();
        }

        private IEnumerable<ITfsChangeset> FetchChangesets(bool byLots, int lastVersion = -1)
        {
            int lowerBoundChangesetId;

            // If we're starting at the Root side of a branch commit (e.g. C1), but there ar
            // invalid commits between C1 and the actual branch side of the commit operation
            // (e.g. a Folder with the branch name was created [C2] and then deleted [C3],
            // then the root-side was branched [C4; C1 --branch--> C4]), this will detecte
            // only the folder creation and deletion operations due to the lowerBound being
            // detected as the root-side of the commit +1 (C1+1=C2) instead of referencing
            // the branch-side of the branching operation [C4].
            if (_properties.InitialChangeset.HasValue || firstChangesetId.HasValue)
            {
                var firstChangesetInBranch = Math.Max(_properties.InitialChangeset ?? int.MinValue, firstChangesetId ?? int.MinValue);
                lowerBoundChangesetId = Math.Max(MaxChangesetId + 1, firstChangesetInBranch);
            }
            else
                lowerBoundChangesetId = MaxChangesetId + 1;
            Trace.WriteLine(RemoteRef + ": Getting changesets from " + lowerBoundChangesetId +
                " to " + lastVersion + " ...", "info");
            if (!IsSubtreeOwner)
                return Tfs.GetChangesets(TfsRepositoryPath, lowerBoundChangesetId, this, lastVersion, byLots);

            return _globals.Repository.GetSubtrees(this)
                .SelectMany(x => Tfs.GetChangesets(x.TfsRepositoryPath, lowerBoundChangesetId, x, lastVersion, byLots))
                .OrderBy(x => x.Summary.ChangesetId);
        }

        public ITfsChangeset GetChangeset(int changesetId)
        {
            return Tfs.GetChangeset(changesetId, this);
        }

        private ITfsChangeset GetLatestChangeset()
        {
            if (!string.IsNullOrEmpty(TfsRepositoryPath))
                return Tfs.GetLatestChangeset(this);
            var changesetId = _globals.Repository.GetSubtrees(this).Select(x => Tfs.GetLatestChangeset(x)).Max(x => x.Summary.ChangesetId);
            return GetChangeset(changesetId);
        }

        private int GetLatestChangesetId()
        {
            if (!string.IsNullOrEmpty(TfsRepositoryPath))
                return Tfs.GetLatestChangesetId(this);
            return _globals.Repository.GetSubtrees(this).Select(x => Tfs.GetLatestChangesetId(x)).Max();
        }

        public void UpdateTfsHead(string commitHash, int changesetId)
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
            Trace.TraceInformation("C" + MaxChangesetId + " = " + MaxCommitHash);
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
            Trace.WriteLine("GC Countdown: " + _globals.GcCountdown);
            if (--_globals.GcCountdown < 0)
            {
                _globals.GcCountdown = _globals.GcPeriod;
                Repository.GarbageCollect(true, "Try running it after git-tfs is finished.");
            }
        }

        private LogEntry Apply(string parent, ITfsChangeset changeset, IDictionary<string, GitObject> entries)
        {
            return Apply(parent, changeset, entries, null);
        }

        private LogEntry Apply(string parent, ITfsChangeset changeset, Action<Exception> ignorableErrorHandler)
        {
            return Apply(parent, changeset, BuildEntryDictionary(), ignorableErrorHandler);
        }

        private LogEntry Apply(string parent, ITfsChangeset changeset, IDictionary<string, GitObject> entries, Action<Exception> ignorableErrorHandler)
        {
            LogEntry result = null;
            WithWorkspace(changeset.Summary, workspace =>
            {
                var treeBuilder = workspace.Remote.Repository.GetTreeBuilder(parent);
                result = changeset.Apply(parent, treeBuilder, workspace, entries, ignorableErrorHandler);
                result.Tree = treeBuilder.GetTree();
            });
            if (!string.IsNullOrEmpty(parent)) result.CommitParents.Add(parent);
            return result;
        }

        private LogEntry CopyTree(string lastCommit, ITfsChangeset changeset)
        {
            LogEntry result = null;
            WithWorkspace(changeset.Summary, workspace =>
            {
                var treeBuilder = workspace.Remote.Repository.GetTreeBuilder(null);
                result = changeset.CopyTree(treeBuilder, workspace);
                result.Tree = treeBuilder.GetTree();
            });
            if (!string.IsNullOrEmpty(lastCommit)) result.CommitParents.Add(lastCommit);
            return result;
        }

        private string Commit(LogEntry logEntry)
        {
            logEntry.Log = BuildCommitMessage(logEntry.Log, logEntry.ChangesetId);
            return Repository.Commit(logEntry).Sha;
        }

        private string BuildCommitMessage(string tfsCheckinComment, int changesetId)
        {
            var builder = new StringWriter();
            builder.WriteLine(tfsCheckinComment);
            builder.WriteLine(GitTfsConstants.TfsCommitInfoFormat,
                TfsUrl, TfsRepositoryPath, changesetId);
            return builder.ToString();
        }

        public void Unshelve(string shelvesetOwner, string shelvesetName, string destinationBranch, Action<Exception> ignorableErrorHandler, bool force)
        {
            var destinationRef = GitRepository.ShortToLocalName(destinationBranch);
            if (Repository.HasRef(destinationRef))
                throw new GitTfsException("ERROR: Destination branch (" + destinationBranch + ") already exists!");

            var shelvesetChangeset = Tfs.GetShelvesetData(this, shelvesetOwner, shelvesetName);

            var parentId = shelvesetChangeset.BaseChangesetId;
            var ch = GetTfsChangesetById(parentId);
            string rootCommit;
            if (ch == null)
            {
                if (!force)
                    throw new GitTfsException("ERROR: Parent changeset C" + parentId + " not found.", new[]
                            {
                                "Try fetching the latest changes from TFS",
                                "Try applying the shelveset on the currently checkouted commit using the '--force' option"
                            }
                        );
                Trace.TraceInformation("warning: Parent changeset C" + parentId + " not found."
                                 + " Trying to apply the shelveset on the current commit...");
                rootCommit = Repository.GetCurrentCommit();
            }
            else
            {
                rootCommit = ch.GitCommit;
            }

            var log = Apply(rootCommit, shelvesetChangeset, ignorableErrorHandler);
            var commit = Commit(log);
            Repository.UpdateRef(destinationRef, commit, "Shelveset " + shelvesetName + " from " + shelvesetOwner);
        }

        public void Shelve(string shelvesetName, string head, TfsChangesetInfo parentChangeset, CheckinOptions options, bool evaluateCheckinPolicies)
        {
            WithWorkspace(parentChangeset, workspace => Shelve(shelvesetName, head, parentChangeset, options, evaluateCheckinPolicies, workspace));
        }

        public bool HasShelveset(string shelvesetName)
        {
            return Tfs.HasShelveset(shelvesetName);
        }

        private void Shelve(string shelvesetName, string head, TfsChangesetInfo parentChangeset, CheckinOptions options, bool evaluateCheckinPolicies, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parentChangeset.GitCommit, workspace);
            workspace.Shelve(shelvesetName, evaluateCheckinPolicies, options, () => Repository.GetCommitMessage(head, parentChangeset.GitCommit));
        }

        public int CheckinTool(string head, TfsChangesetInfo parentChangeset)
        {
            var changeset = 0;
            WithWorkspace(parentChangeset, workspace => changeset = CheckinTool(head, parentChangeset, workspace));
            return changeset;
        }

        private int CheckinTool(string head, TfsChangesetInfo parentChangeset, ITfsWorkspace workspace)
        {
            PendChangesToWorkspace(head, parentChangeset.GitCommit, workspace);
            return workspace.CheckinTool(() => Repository.GetCommitMessage(head, parentChangeset.GitCommit));
        }

        private void PendChangesToWorkspace(string head, string parent, ITfsWorkspaceModifier workspace)
        {
            using (var tidyWorkspace = new DirectoryTidier(workspace, () => GetLatestChangeset().GetFullTree()))
            {
                foreach (var change in Repository.GetChangedFiles(parent, head))
                {
                    change.Apply(tidyWorkspace);
                }
            }
        }

        public int Checkin(string head, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null)
        {
            var changeset = 0;
            WithWorkspace(parentChangeset, workspace => changeset = Checkin(head, parentChangeset.GitCommit, workspace, options, sourceTfsPath));
            return changeset;
        }

        public int Checkin(string head, string parent, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null)
        {
            var changeset = 0;
            WithWorkspace(parentChangeset, workspace => changeset = Checkin(head, parent, workspace, options, sourceTfsPath));
            return changeset;
        }

        private void WithWorkspace(TfsChangesetInfo parentChangeset, Action<ITfsWorkspace> action)
        {
            //are there any subtrees?
            var subtrees = _globals.Repository.GetSubtrees(this);
            if (subtrees.Any())
            {
                Tfs.WithWorkspace(WorkingDirectory, this, subtrees.Select(x => new Tuple<string, string>(x.TfsRepositoryPath, x.Prefix)), parentChangeset, action);
            }
            else
            {
                Tfs.WithWorkspace(WorkingDirectory, this, parentChangeset, action);
            }
        }

        private int Checkin(string head, string parent, ITfsWorkspace workspace, CheckinOptions options, string sourceTfsPath)
        {
            PendChangesToWorkspace(head, parent, workspace);
            if (!string.IsNullOrWhiteSpace(sourceTfsPath))
                workspace.Merge(sourceTfsPath, TfsRepositoryPath);
            return workspace.Checkin(options, () => Repository.GetCommitMessage(head, parent));
        }

        public bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath)
        {
            if (!MatchesTfsUrl(tfsUrl))
                return false;

            if (TfsRepositoryPath == null)
                return tfsRepositoryPath == null;

            return TfsRepositoryPath.Equals(tfsRepositoryPath, StringComparison.OrdinalIgnoreCase);
        }

        public void DeleteShelveset(string shelvesetName)
        {
            WithWorkspace(null, workspace => workspace.DeleteShelveset(shelvesetName));
        }

        private bool MatchesTfsUrl(string tfsUrl)
        {
            return TfsUrl.Equals(tfsUrl, StringComparison.OrdinalIgnoreCase) || Aliases.Contains(tfsUrl, StringComparison.OrdinalIgnoreCase);
        }

        private string ExtractGitBranchNameFromTfsRepositoryPath(string tfsRepositoryPath)
        {
            var includeTeamProjectName = !Repository.IsInSameTeamProjectAsDefaultRepository(tfsRepositoryPath);
            var gitBranchName = tfsRepositoryPath.ToGitBranchNameFromTfsRepositoryPath(includeTeamProjectName);
            gitBranchName = Repository.AssertValidBranchName(gitBranchName);
            Trace.TraceInformation("The name of the local branch will be : " + gitBranchName);
            return gitBranchName;
        }

        public IGitTfsRemote InitBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, int rootChangesetId, bool fetchParentBranch, string gitBranchNameExpected = null, IRenameResult renameResult = null)
        {
            return InitTfsBranch(remoteOptions, tfsRepositoryPath, rootChangesetId, fetchParentBranch, gitBranchNameExpected, renameResult);
        }

        private bool IgnoreException(string message, bool ignoreRestricted, bool printHint = true)
        {
            // Detect exception "TF14098: Access Denied: User ??? needs
            // Read permission(s) for at least one item in changeset ???."
            if (message.Contains("TF14098"))
            {
                if (ignoreRestricted)
                    return true;
                if (printHint)
                    Trace.TraceWarning("\nAccess to changeset denied. Try the '--ignore-restricted-changesets' option!\n");
            }
            return false;
        }

        private IGitTfsRemote InitTfsBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, int rootChangesetId = -1, bool fetchParentBranch = false, string gitBranchNameExpected = null, IRenameResult renameResult = null, bool ignoreRestricted = false)
        {
            Trace.WriteLine("Begin process of creating branch for remote :" + tfsRepositoryPath);
            // TFS string representations of repository paths do not end in trailing slashes
            tfsRepositoryPath = (tfsRepositoryPath ?? string.Empty).TrimEnd('/');

            string gitBranchName = ExtractGitBranchNameFromTfsRepositoryPath(
                string.IsNullOrWhiteSpace(gitBranchNameExpected) ? tfsRepositoryPath : gitBranchNameExpected);
            if (string.IsNullOrWhiteSpace(gitBranchName))
                throw new GitTfsException("error: The Git branch name '" + gitBranchName + "' is not valid...\n");
            Trace.WriteLine("Git local branch will be :" + gitBranchName);

            string sha1RootCommit = null;
            if (rootChangesetId != -1)
            {
                sha1RootCommit = Repository.FindCommitHashByChangesetId(rootChangesetId);
                if (fetchParentBranch && string.IsNullOrWhiteSpace(sha1RootCommit))
                {
                    try
                    {
                        sha1RootCommit = FindRootRemoteAndFetch(rootChangesetId, renameResult);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Getting changeset fetch failed: " + ex.Message);
                        if (!IgnoreException(ex.Message, ignoreRestricted))
                            throw;
                    }
                }

                if (string.IsNullOrWhiteSpace(sha1RootCommit))
                    return null;

                Trace.WriteLine("Found commit " + sha1RootCommit + " for changeset :" + rootChangesetId);
            }

            IGitTfsRemote tfsRemote;
            if (Repository.HasRemote(gitBranchName))
            {
                Trace.WriteLine("Remote already exist");
                tfsRemote = Repository.ReadTfsRemote(gitBranchName);
                if (tfsRemote.TfsUrl != TfsUrl)
                    Trace.WriteLine("warning: Url is different");
                if (tfsRemote.TfsRepositoryPath != tfsRepositoryPath)
                    Trace.WriteLine("warning: TFS repository path is different");
            }
            else
            {
                Trace.WriteLine("Try creating remote...");
                tfsRemote = Repository.CreateTfsRemote(new RemoteInfo
                {
                    Id = gitBranchName,
                    Url = TfsUrl,
                    Repository = tfsRepositoryPath,
                    RemoteOptions = remoteOptions
                }, string.Empty);
                tfsRemote.ExportMetadatas = ExportMetadatas;
                tfsRemote.ExportWorkitemsMapping = ExportWorkitemsMapping;
            }
            if (sha1RootCommit != null && !Repository.HasRef(tfsRemote.RemoteRef))
            {
                if (!Repository.CreateBranch(tfsRemote.RemoteRef, sha1RootCommit))
                    throw new GitTfsException("error: Fail to create remote branch ref file!");
            }
            Trace.WriteLine("Remote created!");
            return tfsRemote;
        }
    }
}
