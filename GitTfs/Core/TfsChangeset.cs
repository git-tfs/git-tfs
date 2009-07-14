using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core
{
    class TfsChangeset : ITfsChangeset
    {
        private readonly TfsHelper tfs;
        private readonly Changeset changeset;
        public TfsChangesetInfo Summary { get; set; }

        public LogEntry Apply(GitTfsRemote remote, string lastChangeset, GitIndexInfo index)
        {
            foreach(var change in changeset.Changes)
            {
                var pathInGitRepo = remote.GetPathInGitRepo(change.Item.ServerItem);
                if(pathInGitRepo == null || remote.IsIgnored(pathInGitRepo))
                    continue;
                if (change.ChangeType.IncludesOneOf(ChangeType.Add, ChangeType.Edit, ChangeType.Rename, ChangeType.Undelete, ChangeType.Branch, ChangeType.Merge))
                {
                    if (change.Item.ItemType == ItemType.File)
                    {
                        /////////////////
                        // just add is implemented right now:
                        var mode = GetCurrentMode(remote.Repository, lastChangeset, pathInGitRepo);
                        if (mode == null || change.ChangeType.IncludesOneOf(ChangeType.Add))
                            mode = "100644";
                        index.Update(mode, pathInGitRepo, change.Item.DownloadFile());
                    }
                    /////////////////
                    //if(change.ChangeType.IncludesOneOf(ChangeType.Rename))
                    //{
                    //    var oldPath = GetPathBeforeRename(change);
                    //    if(oldPath != null) index.Remove(oldPath);
                    //}
                    //using(var changeStream = change.Item.DownloadFile())
                    //{
                    //    index.Update(GetMode(change), pathInGitRepo, changeStream);
                    //}
                }
                else if (change.ChangeType.IncludesOneOf(ChangeType.Delete))
                {
                    var treeInfo = remote.Repository.Command("ls-tree", "-z", lastChangeset, "./" + pathInGitRepo);
                    var treeRegex =
                        new Regex("\\A040000 tree (?<tree>" + GitTfsConstants.Sha1 + ") \\t\\Q" + pathInGitRepo + "\\E\0");
                    var match = treeRegex.Match(treeInfo);
                    if(match.Success)
                    {
                        remote.Repository.CommandOutputPipe(stdout =>
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
                else
                {
                    Trace.WriteLine("Skipping changeset " + changeset.ChangesetId + " change to " +
                                    change.Item.ServerItem + " of type " + change.ChangeType + ".");
                }
            }
            return MakeNewLogEntry();
        }

        private string GetCurrentMode(IGitRepository repository, string lastChangeset, string item)
        {
            if(String.IsNullOrEmpty(lastChangeset)) return null;
            var treeInfo = repository.Command("ls-tree", "-z", lastChangeset, "./" + item);
            var treeRegex =
                new Regex("\\A(?<mode>\\d{6}) blob (?<blob>" + GitTfsConstants.Sha1 + ")\\t" + Regex.Escape(item) + "\0");
            var match = treeRegex.Match(treeInfo);
            return !match.Success ? null : match.Groups["mode"].Value;
        }

        private string GetPathBeforeRename(Change change)
        {
            throw new NotImplementedException();
        }

        private LogEntry MakeNewLogEntry()
        {
            var log = new LogEntry();
            log.CommitterName = log.AuthorName = GetAuthorName();
            log.CommitterEmail = log.AuthorEmail = GetAuthorEmail();
            log.Date = changeset.CreationDate;
            log.Log = changeset.Comment + Environment.NewLine;
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

        public TfsChangeset(TfsHelper tfs, Changeset changeset)
        {
            this.tfs = tfs;
            this.changeset = changeset;
        }
    }
}
