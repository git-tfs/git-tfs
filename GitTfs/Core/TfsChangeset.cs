using System;
using System.Diagnostics;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core
{
    class TfsChangeset : ITfsChangeset
    {
        private readonly TfsHelper tfs;
        private readonly Changeset changeset;
        public TfsChangesetInfo Summary { get; set; }

        public LogEntry Apply(GitIndexInfo index)
        {
            foreach(var change in changeset.Changes)
            {
                if (change.ChangeType.IncludesOneOf(ChangeType.Add, ChangeType.Edit, ChangeType.Undelete, ChangeType.Branch, ChangeType.Merge))
                {
                    throw new NotImplementedException("TODO: add/edit");
                }
                else if(change.ChangeType.IncludesOneOf(ChangeType.Rename))
                {
                    throw new NotImplementedException("TODO: rename");
                }
                else if (change.ChangeType.IncludesOneOf(ChangeType.Delete))
                {
                    throw new NotImplementedException("TODO: delete");
                }
                else
                {
                    Trace.WriteLine("Skipping changeset " + changeset.ChangesetId + " change to " +
                                    change.Item.ServerItem + " of type " + change.ChangeType + ".");
                }
            }
            //throw new System.NotImplementedException();
            return new LogEntry();
        }

        public TfsChangeset(TfsHelper tfs, Changeset changeset)
        {
            this.tfs = tfs;
            this.changeset = changeset;
        }
    }
}
