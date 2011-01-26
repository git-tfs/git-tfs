using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public interface IGitTfsRemote
    {
        string Id { get; set; }
        string TfsRepositoryPath { get; set; }
        string IgnoreRegexExpression { get; set; }
        IGitRepository Repository { get; set; }
        ITfsHelper Tfs { get; set; }
        long MaxChangesetId { get; set; }
        string MaxCommitHash { get; set; }
        string RemoteRef { get; }
        bool ShouldSkip(string path);
        string GetPathInGitRepo(string tfsPath);
        void Fetch(Dictionary<long, string> mergeInfo);
        void QuickFetch();
        void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies);
        bool HasShelveset(string shelvesetName);
        void CheckinTool(string head, TfsChangesetInfo parentChangeset);
        long Checkin(string treeish, TfsChangesetInfo parentChangeset);
    }

    public static partial class Ext
    {
        public static void Fetch(this IGitTfsRemote remote)
        {
            remote.Fetch(new Dictionary<long, string>());
        }
    }
}