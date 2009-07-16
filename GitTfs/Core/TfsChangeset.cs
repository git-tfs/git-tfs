using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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

        public LogEntry Apply(string lastCommit, GitIndexInfo index)
        {
            foreach(var change in changeset.Changes)
            {
                var pathInGitRepo = Summary.Remote.GetPathInGitRepo(change.Item.ServerItem);
                if(pathInGitRepo == null || Summary.Remote.IsIgnored(pathInGitRepo))
                    continue;
                if (change.ChangeType.IncludesOneOf(ChangeType.Add, ChangeType.Edit, ChangeType.Rename, ChangeType.Undelete, ChangeType.Branch, ChangeType.Merge))
                {
                        Update(change, pathInGitRepo, lastCommit, index);
                }
                else if (change.ChangeType.IncludesOneOf(ChangeType.Delete))
                {
                    Delete(pathInGitRepo, index, lastCommit);
                }
                else
                {
                    Trace.WriteLine("Skipping changeset " + changeset.ChangesetId + " change to " +
                                    change.Item.ServerItem + " of type " + change.ChangeType + ".");
                }
            }
            return MakeNewLogEntry();
        }

        private string GetPathBeforeRename(Item item)
        {
            return item.VersionControlServer.GetItem(item.ItemId, item.ChangesetId - 1).ServerItem;
        }

        private void Update(Change change, string pathInGitRepo, string lastCommit, GitIndexInfo index)
        {
            if (change.Item.ItemType == ItemType.File)
            {
                string mode = null;

                // It's VERY convenient that TFS renames every file in the tree, not just the dir.
                // If it didn't, then doing directory renames would be much more involved.
                // Instead, we can just handle each file's change.
                if (change.ChangeType.IncludesOneOf(ChangeType.Rename))
                {
                    var oldPath = Summary.Remote.GetPathInGitRepo(GetPathBeforeRename(change.Item));
                    if (oldPath != null)
                    {
                        mode = GetCurrentMode(lastCommit, oldPath);
                        index.Remove(oldPath);
                    }
                }
                else
                {
                    mode = GetCurrentMode(lastCommit, pathInGitRepo);
                }

                if (mode == null || change.ChangeType.IncludesOneOf(ChangeType.Add))
                    mode = "100644";
                index.Update(mode, pathInGitRepo, change.Item.DownloadFile());
            }
        }

        private void Delete(string pathInGitRepo, GitIndexInfo index, string lastChangeset)
        {
            var treeInfo = Summary.Remote.Repository.Command("ls-tree", "-z", lastChangeset, "./" + pathInGitRepo);
            var treeRegex =
                new Regex("\\A040000 tree (?<tree>" + GitTfsConstants.Sha1 + ") \\t" + Regex.Escape(pathInGitRepo) + "\0");
            var match = treeRegex.Match(treeInfo);
            if(match.Success)
            {
                Summary.Remote.Repository.CommandOutputPipe(stdout =>
                                                        {
                                                            var reader = new DelimitedReader(stdout);
                                                            string fileInDir;
                                                            while((fileInDir = reader.Read()) != null)
                                                            {
                                                                var pathToRemove = pathInGitRepo + "/" +
                                                                                   fileInDir;
                                                                index.Remove(pathToRemove);
                                                                Trace.WriteLine("\tD\t" + pathToRemove);
                                                            }
                                                        }, "ls-tree", "-r", "--name-only", "-z", match.Groups["tree"].Value);
            }
            else
            {
                index.Remove(pathInGitRepo);
            }
            Trace.WriteLine("\tD\t" + pathInGitRepo);
        }

        private string GetCurrentMode(string lastChangeset, string item)
        {
            if(String.IsNullOrEmpty(lastChangeset)) return null;
            var treeInfo = Summary.Remote.Repository.Command("ls-tree", "-z", lastChangeset, "./" + item);
            var treeRegex =
                new Regex("\\A(?<mode>\\d{6}) blob (?<blob>" + GitTfsConstants.Sha1 + ")\\t" + Regex.Escape(item) + "\0");
            var match = treeRegex.Match(treeInfo);
            return !match.Success ? null : match.Groups["mode"].Value;
        }

        private LogEntry MakeNewLogEntry()
        {
            var log = new LogEntry();
            log.CommitterName = log.AuthorName = GetAuthorName();
            log.CommitterEmail = log.AuthorEmail = GetAuthorEmail();
            log.Date = changeset.CreationDate;
            log.Log = changeset.Comment + Environment.NewLine;
            log.ChangesetId = changeset.ChangesetId;
            return log;
        }

        private string GetAuthorEmail()
        {
            var identity = tfs.GetIdentity(changeset.Committer);
            return identity.MailAddress;
        }

        private string GetAuthorName()
        {
            var identity = tfs.GetIdentity(changeset.Committer);
            return identity.DisplayName;
        }
    }
}
