using System;
using System.Collections.Generic;
using GitTfs.Core.TfsInterop;
using GitTfs.Commands;

namespace GitTfs.Core
{
    public interface IFetchResult : IRenameResult
    {
        bool IsSuccess { get; set; }
        int LastFetchedChangesetId { get; set; }
        int NewChangesetCount { get; set; }
        string ParentBranchTfsPath { get; set; }
    }

    public interface IRenameResult
    {
        bool IsProcessingRenameChangeset { get; set; }
        string LastParentCommitBeforeRename { get; set; }
    }

    // Internal WorItem representation for export scenarios
    public interface IExportWorkItem
    {
        string Id { get; }
        string Title { get; }
    }

    public interface IGitTfsRemote
    {
        bool IsDerived { get; }
        RemoteInfo RemoteInfo { get; }
        string Id { get; }
        string TfsUrl { get; }
        string TfsRepositoryPath { get; }
        /// <summary>
        /// Gets the TFS server-side paths of all subtrees of this remote.
        /// Valid if the remote has subtrees, which occurs when <see cref="TfsRepositoryPath"/> is null.
        /// </summary>
        string[] TfsSubtreePaths { get; }
        string IgnoreRegexExpression { get; }
        string IgnoreExceptRegexExpression { get; }
        bool Autotag { get; }
        string TfsUsername { get; set; }
        string TfsPassword { get; set; }
        IGitRepository Repository { get; }
        ITfsHelper Tfs { get; }
        int MaxChangesetId { get; set; }
        string MaxCommitHash { get; set; }
        string RemoteRef { get; }
        bool IsSubtree { get; }
        bool IsSubtreeOwner { get; }
        string OwningRemoteId { get; }
        string Prefix { get; }
        bool ExportMetadatas { get; set; }
        Dictionary<string, IExportWorkItem> ExportWorkitemsMapping { get; set; }
        int? GetFirstChangeset();
        void SetFirstChangeset(int? changesetId);
        bool ShouldSkip(string path);
        bool IsIgnored(string path);
        bool IsInDotGit(string path);
        IGitTfsRemote InitBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, int rootChangesetId = -1, bool fetchParentBranch = false, string gitBranchNameExpected = null, IRenameResult renameResult = null);
        string GetPathInGitRepo(string tfsPath);
        IFetchResult Fetch(bool stopOnFailMergeCommit = false, int lastChangesetIdToFetch = -1, IRenameResult renameResult = null);
        IFetchResult FetchWithMerge(int mergeChangesetId, bool stopOnFailMergeCommit = false, IRenameResult renameResult = null, params string[] parentCommitsHashes);
        void QuickFetch(int changesetId, bool ignoreRestricted, bool printRestrictionHint = true);
        void Unshelve(string shelvesetOwner, string shelvesetName, string destinationBranch, Action<Exception> ignorableErrorHandler, bool force);
        void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, CheckinOptions options, bool evaluateCheckinPolicies);
        bool HasShelveset(string shelvesetName);
        int CheckinTool(string head, TfsChangesetInfo parentChangeset);
        int Checkin(string treeish, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null);

        /// <summary>
        /// Checks in to TFS set of changes from git repository between given commits (parent..head) onto given TFS changeset. Returns ID of the new changeset.
        /// </summary>
        int Checkin(string head, string parent, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null);
        void CleanupWorkspace();
        void CleanupWorkspaceDirectory();
        ITfsChangeset GetChangeset(int changesetId);
        void UpdateTfsHead(string commitHash, int changesetId);
        void EnsureTfsAuthenticated();
        bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath);
        void DeleteShelveset(string shelvesetName);
    }

    public static class IGitTfsRemoteExt
    {
        public static IFetchResult FetchWithMerge(this IGitTfsRemote remote, int mergeChangesetId, bool stopOnFailMergeCommit = false, params string[] parentCommitsHashes)
        {
            return remote.FetchWithMerge(mergeChangesetId, stopOnFailMergeCommit, null, parentCommitsHashes);
        }
    }
}
