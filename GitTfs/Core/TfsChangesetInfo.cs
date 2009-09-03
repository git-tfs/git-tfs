namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo
    {
        public IGitTfsRemote Remote { get; set; }
        public long ChangesetId { get; set; }
        public string GitCommit { get; set; }
    }
}
