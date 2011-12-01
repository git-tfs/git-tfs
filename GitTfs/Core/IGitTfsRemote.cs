using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public interface IGitTfsRemote
    {
        bool IsDerived { get; }
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
        void Fetch();
        void FetchWithMerge(long mergeChangesetId, params string[] parentCommitsHashes);
        void QuickFetch();
        void QuickFetch(int changesetId);
        void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies);
        bool HasShelveset(string shelvesetName);
        long CheckinTool(string head, TfsChangesetInfo parentChangeset);
        long Checkin(string treeish, TfsChangesetInfo parentChangeset);
        void CleanupWorkspace();
        ITfsChangeset GetChangeset(long changesetId);
        void UpdateRef(string commitHash, long changesetId);
        void EnsureTfsAuthenticated();
    }
}
