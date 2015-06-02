using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{

    public interface IFetchResult : IRenameResult
    {
        bool IsSuccess { get; set; }
        long LastFetchedChangesetId { get; set; }
        int NewChangesetCount { get; set; }
        string ParentBranchTfsPath { get; set; }
    }

    public interface IRenameResult
    {
        bool IsProcessingRenameChangeset { get; set; }
        string LastParentCommitBeforeRename { get; set; }
    }

    public interface IGitTfsRemote
    {
        bool IsDerived { get; }
        RemoteInfo RemoteInfo { get; }
        string Id { get; set; }
        string TfsUrl { get; set; }
        string TfsRepositoryPath { get; set; }
        /// <summary>
        /// Gets the TFS server-side paths of all subtrees of this remote.
        /// Valid if the remote has subtrees, which occurs when <see cref="TfsRepositoryPath"/> is null.
        /// </summary>
        string[] TfsSubtreePaths { get; }
        string IgnoreRegexExpression { get; set; }
        string IgnoreExceptRegexExpression { get; set; }
        bool Autotag { get; set; }
        string TfsUsername { get; set; }
        string TfsPassword { get; set; }
        IGitRepository Repository { get; set; }
        [Obsolete("Make this go away")]
        ITfsHelper Tfs { get; set; }
        long MaxChangesetId { get; set; }
        string MaxCommitHash { get; set; }
        string RemoteRef { get; }
        bool IsSubtree { get; }
        bool IsSubtreeOwner { get; }
        string OwningRemoteId { get; }
        string Prefix { get; }
        bool ExportMetadatas { get; set; }
        Dictionary<string, string> ExportWorkitemsMapping { get; set; }
        bool ShouldSkip(string path);
        IGitTfsRemote InitBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, long rootChangesetId = -1, bool fetchParentBranch = false, string gitBranchNameExpected = null, IRenameResult renameResult = null);
        string GetPathInGitRepo(string tfsPath);
        IFetchResult Fetch(bool stopOnFailMergeCommit = false, IRenameResult renameResult = null);
        IFetchResult FetchWithMerge(long mergeChangesetId, bool stopOnFailMergeCommit = false, IRenameResult renameResult = null, params string[] parentCommitsHashes);
        void QuickFetch();
        void QuickFetch(int changesetId);
        void Unshelve(string shelvesetOwner, string shelvesetName, string destinationBranch, Action<Exception> ignorableErrorHandler, bool force);
        void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies);
        bool HasShelveset(string shelvesetName);
        long CheckinTool(string head, TfsChangesetInfo parentChangeset);
        long Checkin(string treeish, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null);

        /// <summary>
        /// Checks in to TFS set of changes from git repository between given commits (parent..head) onto given TFS changeset. Returns ID of the new changeset.
        /// </summary>
        long Checkin(string head, string parent, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null);
        void CleanupWorkspace();
        void CleanupWorkspaceDirectory();
        ITfsChangeset GetChangeset(long changesetId);
        void UpdateTfsHead(string commitHash, long changesetId);
        void EnsureTfsAuthenticated();
        bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath);
    }

    public static class IGitTfsRemoteExt
    {
        public static IFetchResult FetchWithMerge(this IGitTfsRemote remote, long mergeChangesetId, bool stopOnFailMergeCommit = false, params string[] parentCommitsHashes)
        {
            return remote.FetchWithMerge(mergeChangesetId, stopOnFailMergeCommit, null, parentCommitsHashes);
        }
    }
}
