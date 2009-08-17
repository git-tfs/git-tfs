using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Util;

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
            var initialTree = Summary.Remote.Repository.GetObjects(lastCommit);
            foreach (var change in Sort(changeset.Changes))
            {
                Apply(change, index, initialTree);
            }
            return MakeNewLogEntry();
        }

        private void Apply(Change change, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            // If you make updates to a dir in TF, the changeset includes changes for all the children also,
            // and git doesn't really care if you add or delete empty dirs.
            if (change.Item.ItemType == ItemType.File)
            {
                var pathInGitRepo = Summary.Remote.GetPathInGitRepo(change.Item.ServerItem);
                if (pathInGitRepo == null || Summary.Remote.ShouldSkip(pathInGitRepo))
                    return;
                if (change.ChangeType.IncludesOneOf(ChangeType.Rename))
                {
                    Rename(change, pathInGitRepo, index, initialTree);
                }
                else if (change.ChangeType.IncludesOneOf(ChangeType.Delete))
                {
                    Delete(pathInGitRepo, index, initialTree);
                }
                else
                {
                    Update(change, pathInGitRepo, index, initialTree);
                }
            }
        }

        private void Rename(Change change, string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            var oldPath = Summary.Remote.GetPathInGitRepo(GetPathBeforeRename(change.Item));
            if (oldPath != null)
            {
                Delete(oldPath, index, initialTree);
            }
            if (!change.ChangeType.IncludesOneOf(ChangeType.Delete))
            {
                Update(change, pathInGitRepo, index, initialTree);
            }
        }

        private IEnumerable<Change> Sort(IEnumerable<Change> changes)
        {
            return changes.OrderBy(change => Rank(change.ChangeType));
        }

        private int Rank(ChangeType type)
        {
            if (type.IncludesOneOf(ChangeType.Delete))
                return 0;
            if (type.IncludesOneOf(ChangeType.Rename))
                return 1;
            return 2;
        }

        private string GetPathBeforeRename(Item item)
        {
            return item.VersionControlServer.GetItem(item.ItemId, item.ChangesetId - 1).ServerItem;
        }

        private void Update(Change change, string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            if (change.Item.DeletionId == 0)
            {
                using (var tempFile = new TemporaryFile())
                {
                    change.Item.DownloadFile(tempFile);
                    index.Update(GetMode(change, initialTree, pathInGitRepo),
                                 UpdateDirectoryToMatchExtantCasing(pathInGitRepo, initialTree),
                                 tempFile);
                }
            }
        }

        private string GetMode(Change change, IDictionary<string, GitObject> initialTree, string pathInGitRepo)
        {
            if(initialTree.ContainsKey(pathInGitRepo) && !change.ChangeType.IncludesOneOf(ChangeType.Add))
            {
                return initialTree[pathInGitRepo].Mode;
            }
            return "100644";
        }

        private static readonly Regex pathWithDirRegex = new Regex("(?<dir>.*)/(?<file>[^/]+)");

        private string UpdateDirectoryToMatchExtantCasing(string pathInGitRepo, IDictionary<string, GitObject> initialTree)
        {
            string newPathTail = null;
            string newPathHead = pathInGitRepo;
            while(true)
            {
                if(initialTree.ContainsKey(newPathHead))
                {
                    return MaybeAppendPath(initialTree[newPathHead].Path, newPathTail);
                }
                var pathWithDirMatch = pathWithDirRegex.Match(newPathHead);
                if(!pathWithDirMatch.Success)
                {
                    return MaybeAppendPath(newPathHead, newPathTail);
                }
                newPathTail = MaybeAppendPath(pathWithDirMatch.Groups["file"].Value, newPathTail);
                newPathHead = pathWithDirMatch.Groups["dir"].Value;
            }
        }

        private string MaybeAppendPath(string path, object tail)
        {
            if(tail != null)
                path = path + "/" + tail;
            return path;
        }

        private void Delete(string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            if(initialTree.ContainsKey(pathInGitRepo))
            {
                index.Remove(initialTree[pathInGitRepo].Path);
                Trace.WriteLine("\tD\t" + pathInGitRepo);
            }
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
