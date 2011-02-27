using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    // This isn't intended to ever be a command. The intent is that
    // you create a repository with quick-clone, and then use
    // fetch to stay up-to-date.
    //
    // This cannot be a command until the following are sorted out:
    //  1. How to choose a parent commit.
    //  2. Load the correct set of extant casing.
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
