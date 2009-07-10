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

        public LogEntry Apply(GitTfsRemote remote, GitIndexInfo index)
        {
            foreach(var change in changeset.Changes)
            {
                var pathInGitRepo = remote.GetPathInGitRepo(change.Item.ServerItem);
                if(pathInGitRepo == null || remote.IsIgnored(pathInGitRepo))
                    continue;
                if (change.ChangeType.IncludesOneOf(ChangeType.Add, ChangeType.Edit, ChangeType.Rename, ChangeType.Undelete, ChangeType.Branch, ChangeType.Merge))
                {
                    if(change.ChangeType.IncludesOneOf(ChangeType.Rename))
                    {
                        var oldPath = GetPathBeforeRename(change);
                        if(oldPath != null) index.Remove(oldPath);
                    }
                    using(var changeStream = GetStream(change))
                    {
                      index.Update(GetMode(change), pathInGitRepo, changeStream);
                    }
                }
                else if (change.ChangeType.IncludesOneOf(ChangeType.Delete))
                {
                    index.Remove(pathInGitRepo);
                }
                else
                {
                    Trace.WriteLine("Skipping changeset " + changeset.ChangesetId + " change to " +
                                    change.Item.ServerItem + " of type " + change.ChangeType + ".");
                }
            }
            return MakeNewLogEntry();
        }

        private string GetMode(Change change)
        {
        }

        private LogEntry MakeNewLogEntry()
        {
            var log = new LogEntry();
            log.CommitterName = log.AuthorName = GetAuthorName();
            log.CommitterEmail = log.AuthorEmail = GetAuthorEmail();
            log.Date = changeset.Date;
            log.Log = changeset.Description + Environment.NewLine;
        }

        public TfsChangeset(TfsHelper tfs, Changeset changeset)
        {
            this.tfs = tfs;
            this.changeset = changeset;
        }
    }
}
