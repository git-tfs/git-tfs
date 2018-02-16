using GitTfs.Core;

namespace GitTfs.Commands
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
        private readonly ConfigProperties _properties;
        public QuickFetch(Globals globals, ConfigProperties properties, RemoteOptions remoteOptions)
            : base(globals, properties, remoteOptions, null)
        {
            _properties = properties;
        }

        protected override void DoFetch(IGitTfsRemote remote, bool stopOnFailMergeCommit)
        {
            if (InitialChangeset.HasValue)
                remote.QuickFetch(InitialChangeset.Value, false, false);
            else
                remote.QuickFetch(-1, false, false);
            _properties.InitialChangeset = remote.MaxChangesetId;
            _properties.PersistAllOverrides();
        }
    }
}
