namespace Sep.Git.Tfs.Core
{
    public interface ITfsChangeset
    {
        TfsChangesetInfo Summary { get; }
        LogEntry Apply(GitTfsRemote remote, string lastCommit, GitIndexInfo index);
    }
}
