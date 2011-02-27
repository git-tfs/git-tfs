using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly TextWriter _stdout;
        public TfsChangesetInfo Summary { get; set; }

        public TfsChangeset(ITfsHelper tfs, IChangeset changeset, TextWriter stdout)
        {
            this.tfs = tfs;
            this.changeset = changeset;
            _stdout = stdout;
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
                var pathInGitRepo = GetPathInGitRepo(change.Item.ServerItem, initialTree);
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

        private string GetPathInGitRepo(string tfsPath, IDictionary<string, GitObject> initialTree)
        {
            var pathInGitRepo = Summary.Remote.GetPathInGitRepo(tfsPath);
            if (pathInGitRepo == null)
                return null;
            return UpdateToMatchExtantCasing(pathInGitRepo, initialTree);
        }

        private void Rename(IChange change, string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            var oldPath = GetPathInGitRepo(GetPathBeforeRename(change.Item), initialTree);
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
            var previousChangeset = item.ChangesetId - 1;
            var oldItem = item.VersionControlServer.GetItem(item.ItemId, previousChangeset);
            if (null == oldItem)
            {
                var history = item.VersionControlServer.QueryHistory(item.ServerItem, item.ChangesetId, 0,
                                                                     TfsRecursionType.None, null, 1, previousChangeset,
                                                                     1, true, false, false);
                var previousChange = history.First();
                oldItem = previousChange.Changes[0].Item;
            }
            return oldItem.ServerItem;
        }

        private void Update(IChange change, string pathInGitRepo, GitIndexInfo index, IDictionary<string, GitObject> initialTree)
        {
            if (change.Item.DeletionId == 0)
            {
                using (var tempFile = new TemporaryFile())
                {
                    change.Item.DownloadFile(tempFile);
                    index.Update(GetMode(change, initialTree, pathInGitRepo),
                                 pathInGitRepo,
                                 tempFile);
                }
            }
        }

        public IEnumerable<TfsTreeEntry> GetTree()
        {
            return GetTree(false);
        }

        public IEnumerable<TfsTreeEntry> GetTree(bool includeIgnoredItems)
        {
            var treeInfo = Summary.Remote.Repository.GetObjects();
            foreach (var item in changeset.VersionControlServer.GetItems(Summary.Remote.TfsRepositoryPath, changeset.ChangesetId, TfsRecursionType.Full))
            {
                if (item.ItemType == TfsItemType.File)
                {
                    var pathInGitRepo = GetPathInGitRepo(item.ServerItem, treeInfo);
                    if (pathInGitRepo != null && !Summary.Remote.ShouldSkip(pathInGitRepo))
                    {
                        yield return new TfsTreeEntry(pathInGitRepo, item);
                    }
                }
            }
        }

        public LogEntry CopyTree(GitIndexInfo index)
        {
            var startTime = DateTime.Now;
            var itemsCopied = 0;
            var maxChangesetId = 0;
            foreach (var entry in GetTree())
            {
                Add(entry.Item, entry.FullName, index);
                maxChangesetId = Math.Max(maxChangesetId, entry.Item.ChangesetId);

                itemsCopied++;
                if(DateTime.Now - startTime > TimeSpan.FromSeconds(30))
                {
                    _stdout.WriteLine("" + itemsCopied + " objects created...");
                    startTime = DateTime.Now;
                }
            }
            return MakeNewLogEntry(maxChangesetId == changeset.ChangesetId ? changeset : tfs.GetChangeset(maxChangesetId));
        }

        private void Add(IItem item, string pathInGitRepo, GitIndexInfo index)
        {
            if(item.DeletionId == 0)
            {
                using(var tempFile = new TemporaryFile())
                {
                    item.DownloadFile(tempFile);
                    index.Update(Mode.NewFile, pathInGitRepo, tempFile);
                }
            }
        }

        private string GetMode(IChange change, IDictionary<string, GitObject> initialTree, string pathInGitRepo)
        {
            if(initialTree.ContainsKey(pathInGitRepo) &&
                !String.IsNullOrEmpty(initialTree[pathInGitRepo].Mode) &&
                !change.ChangeType.IncludesOneOf(TfsChangeType.Add))
            {
                return initialTree[pathInGitRepo].Mode;
            }
            return Mode.NewFile;
        }

        private static readonly Regex pathWithDirRegex = new Regex("(?<dir>.*)/(?<file>[^/]+)");

        private string UpdateToMatchExtantCasing(string pathInGitRepo, IDictionary<string, GitObject> initialTree)
        {
            return UpdateToMatchExtantCasing_NEW(pathInGitRepo, initialTree);
        }

        private string UpdateToMatchExtantCasing_NEW(string pathInGitRepo, IDictionary<string, GitObject> initialTree)
        {
            if (initialTree.ContainsKey(pathInGitRepo))
                return initialTree[pathInGitRepo].Path;

            var fullPath = pathInGitRepo;
            var pathWithDirMatch = pathWithDirRegex.Match(pathInGitRepo);
            if (pathWithDirMatch.Success)
            {

                var dirName = pathWithDirMatch.Groups["dir"].Value;
                var fileName = pathWithDirMatch.Groups["file"].Value;
                fullPath = UpdateToMatchExtantCasing_NEW(dirName, initialTree) + "/" + fileName;
            }
            initialTree[fullPath] = new GitObject {Path = fullPath};
            return fullPath;
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
            return MakeNewLogEntry(changeset);
        }

        private LogEntry MakeNewLogEntry(IChangeset changesetToLog)
        {
            var log = new LogEntry();
            var identity = tfs.GetIdentity(changesetToLog.Committer);
            log.CommitterName = log.AuthorName = null != identity ? identity.DisplayName ?? "Unknown TFS user" : changesetToLog.Committer ?? "Unknown TFS user";
            log.CommitterEmail = log.AuthorEmail = null != identity ? identity.MailAddress ?? changesetToLog.Committer : changesetToLog.Committer;
            log.Date = changesetToLog.CreationDate;
            log.Log = changesetToLog.Comment + Environment.NewLine;
            log.ChangesetId = changesetToLog.ChangesetId;
            return log;
        }
    }
}
