namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo
    {
        public GitTfsRemote Remote { get; set; }
        public long ChangesetId { get; set; }
        public string GitCommit { get; set; }
    }
}
