using Sep.Git.Tfs.Core;
using CommandLine.OptParse;

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

        [OptDef(OptValType.ValueOpt, ValueType=typeof(int))]
        [LongOptionName("changeset")]
        [ShortOptionName('c')]
        public int changeSetId { get; set; }

        protected override void DoFetch(IGitTfsRemote remote)
        {
            // JSD: I hate this but OptParse doesn't document how to handle optional values; nullable int would be best.
            if (changeSetId == default(int))
                remote.QuickFetch();
            else
                remote.QuickFetch(changeSetId);
        }
    }
}
