using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core
{
    class TfsChangeset : ITfsChangeset
    {
        private readonly TfsHelper tfs;
        private readonly Changeset changeset;
        public TfsChangesetInfo Summary { get; set; }

        public TfsChangeset(TfsHelper tfs, Changeset changeset)
        {
            this.tfs = tfs;
            this.changeset = changeset;
        }
    }
}
