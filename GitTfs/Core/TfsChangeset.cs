using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangeset : ITfsChangeset
    {
        private readonly ITfsHelper tfs;
        private readonly IChangeset changeset;
        public TfsChangesetInfo Summary { get; set; }

        public TfsChangeset(ITfsHelper tfs, IChangeset changeset)
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

        private void Apply(IChange change, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            // If you make updates to a dir in TF, the changeset includes changes for all the children also,
            // and git doesn't really care if you add or delete empty dirs.
            if (change.Item.ItemType == TfsItemType.File)
            {
                var pathInGitRepo = Summary.Remote.GetPathInGitRepo(change.Item.ServerItem);
                if (pathInGitRepo == null || Summary.Remote.ShouldSkip(pathInGitRepo))
                    return;
                if (change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                {
                    Rename(change, pathInGitRepo, index, initialTree);
                }
                else if (change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                {
                    Delete(pathInGitRepo, index, initialTree);
                }
                else
                {
                    Update(change, pathInGitRepo, index, initialTree);
                }
            }
        }

        private void Rename(IChange change, string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            var oldPath = Summary.Remote.GetPathInGitRepo(GetPathBeforeRename(change.Item));
            if (oldPath != null)
            {
                Delete(oldPath, index, initialTree);
            }
            if (!change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
            {
                Update(change, pathInGitRepo, index, initialTree);
            }
        }

        private IEnumerable<IChange> Sort(IEnumerable<IChange> changes)
        {
            return changes.OrderBy(change => Rank(change.ChangeType));
        }

        private int Rank(TfsChangeType type)
        {
            if (type.IncludesOneOf(TfsChangeType.Delete))
                return 0;
            if (type.IncludesOneOf(TfsChangeType.Rename))
                return 1;
            return 2;
        }

        private string GetPathBeforeRename(IItem item)
        {
            return item.VersionControlServer.GetItem(item.ItemId, item.ChangesetId - 1).ServerItem;
        }

        private void Update(IChange change, string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
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

        public LogEntry CopyTree(GitIndexInfo index)
        {
            var itemsToVisit = new Queue<IItem>();
            itemsToVisit.Enqueue(changeset.VersionControlServer.GetItem(Summary.Remote.TfsRepositoryPath, changeset.ChangesetId));
            while (!itemsToVisit.IsEmpty())
            {
                var item = itemsToVisit.Dequeue();
                if (item.ItemType == TfsItemType.Folder)
                {
                    foreach (var itemInFolder in changeset.VersionControlServer.GetItems(item.ServerItem, changeset.ChangesetId, TfsRecursionType.OneLevel))
                    {
                        if (itemInFolder.ServerItem != item.ServerItem)
                        {
                            itemsToVisit.Enqueue(itemInFolder);
                        }
                    }
                }
                else
                {
                    var pathInGitRepo = Summary.Remote.GetPathInGitRepo(item.ServerItem);
                    if (pathInGitRepo != null && !Summary.Remote.ShouldSkip(pathInGitRepo))
                    {
                        Add(item, pathInGitRepo, index);
                    }
                }
            }
            throw new NotImplementedException("TODO - finish implementing this.");
        }

        private void Add(IItem item, string pathInGitRepo, GitIndexInfo index)
        {
            Console.Out.WriteLine(item.ServerItem + " -> " + pathInGitRepo);
        }

        private string GetMode(IChange change, IDictionary<string, GitObject> initialTree, string pathInGitRepo)
        {
            if(initialTree.ContainsKey(pathInGitRepo) && !change.ChangeType.IncludesOneOf(TfsChangeType.Add))
            {
                return initialTree[pathInGitRepo].Mode;
            }
            return Mode.NewFile;
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
            var identity = tfs.GetIdentity(changeset.Committer);
            log.CommitterName = log.AuthorName = identity.DisplayName ?? "Unknown TFS user";
            log.CommitterEmail = log.AuthorEmail = identity.MailAddress ?? changeset.Committer;
            log.Date = changeset.CreationDate;
            log.Log = changeset.Comment + Environment.NewLine;
            log.ChangesetId = changeset.ChangesetId;
            return log;
        }
    }
}
