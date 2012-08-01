using System;
using System.ComponentModel;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

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
        public QuickFetch(Globals globals, RemoteOptions remoteOptions, AuthorsFile authors) : base(globals, remoteOptions, authors)
        {
        }

        public override OptionSet OptionSet
        {
            get
            {
                return base.OptionSet.Merge(new OptionSet
                {
                    { "c|changeset=", "The changeset to clone from (must be a number)",
                        v => InitialChangeset = Convert.ToInt32(v) },
                });
            }
        }

        private int? InitialChangeset { get; set; }

        protected override void DoFetch(IGitTfsRemote remote)
        {
            if (InitialChangeset.HasValue)
                remote.QuickFetch(InitialChangeset.Value);
            else
                remote.QuickFetch();
        }
    }
}
