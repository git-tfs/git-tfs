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
        void Fetch();
        void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset);
    }
}