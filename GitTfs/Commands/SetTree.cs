namespace Sep.Git.Tfs.Commands
{
    [Pluggable("set-tree")]
    public class SetTree : GitTfsCommand
    {
        private CommitOptions commitOptions;
        private FcOptions fcOptions;
        private RemoteOptions remoteOptions;
    }
}
