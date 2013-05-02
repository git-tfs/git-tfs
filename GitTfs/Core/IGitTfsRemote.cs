using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{

    public interface IFetchResult
    {
        bool IsSuccess { get; set; }
        long LastFetchedChangesetId { get; set; }
        string ParentBranchTfsPath { get; set; }
    }

    public interface IGitTfsRemote
    {
        bool IsDerived { get; }
        RemoteInfo RemoteInfo { get; }
        string Id { get; set; }
        string TfsUrl { get; set; }
        string TfsRepositoryPath { get; set; }
        string IgnoreRegexExpression { get; set; }
        bool Autotag { get; set; }
        string TfsUsername { get; set; }
        string TfsPassword { get; set; }
        IGitRepository Repository { get; set; }
        [Obsolete("Make this go away")]
        ITfsHelper Tfs { get; set; }
        long MaxChangesetId { get; set; }
        string MaxCommitHash { get; set; }
        string RemoteRef { get; }
        bool ShouldSkip(string path);
        string GetPathInGitRepo(string tfsPath);
        IFetchResult Fetch(bool stopOnFailMergeCommit = false);
        IFetchResult FetchWithMerge(long mergeChangesetId, bool stopOnFailMergeCommit = false, params string[] parentCommitsHashes);
        void QuickFetch();
        void QuickFetch(int changesetId);
        void Unshelve(string shelvesetOwner, string shelvesetName, string destinationBranch);
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
        void UpdateRef(string commitHash, long changesetId);
        void EnsureTfsAuthenticated();
        bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath);
    }
}
