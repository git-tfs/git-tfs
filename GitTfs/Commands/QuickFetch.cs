using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    public class QuickFetch : Fetch
    {
        public QuickFetch(Globals globals, RemoteOptions remoteOptions, FcOptions fcOptions) : base(globals, remoteOptions, fcOptions)
        {
        }

        protected override void DoFetch(IGitTfsRemote remote)
        {
            remote.QuickFetch();
        }
    }
}
