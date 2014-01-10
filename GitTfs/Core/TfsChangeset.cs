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
        private readonly ITfsHelper _tfs;
        private readonly IChangeset _changeset;
        private readonly TextWriter _stdout;
        private readonly AuthorsFile _authors;
        public TfsChangesetInfo Summary { get; set; }
        public int BaseChangesetId { get; set; }

        public TfsChangeset(ITfsHelper tfs, IChangeset changeset, TextWriter stdout, AuthorsFile authors)
        {
            _tfs = tfs;
            _changeset = changeset;
            _stdout = stdout;
            _authors = authors;
            BaseChangesetId = _changeset.Changes.Max(c => c.Item.ChangesetId) - 1;
        }

        public LogEntry Apply(string lastCommit, IGitTreeBuilder treeBuilder, ITfsWorkspace workspace)
        {
            var initialTree = workspace.Remote.Repository.GetObjects(lastCommit);
            workspace.Get(_changeset);
            foreach (var change in Sort(_changeset.Changes))
            {
                Apply(change, treeBuilder, workspace, initialTree);
            }
            return MakeNewLogEntry();
        }

        private void Apply(IChange change, IGitTreeBuilder treeBuilder, ITfsWorkspace workspace, IGitTreeInformation initialTree)
        {
            // If you make updates to a dir in TF, the changeset includes changes for all the children also,
            // and git doesn't really care if you add or delete empty dirs.
            if (change.Item.ItemType == TfsItemType.File)
            {
                var pathInGitRepo = GetPathInGitRepo(change.Item.ServerItem, workspace.Remote);
                if (pathInGitRepo == null || Summary.Remote.ShouldSkip(pathInGitRepo))
                    return;
                if (change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                {
                    Rename(change, pathInGitRepo, treeBuilder, workspace, initialTree);
                }
                else if (change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                {
                    Delete(pathInGitRepo, treeBuilder, initialTree);
                }
                else
                {
                    Update(change, pathInGitRepo, treeBuilder, workspace, initialTree);
                }
            }
        }

        private string GetPathInGitRepo(string tfsPath, IGitTfsRemote remote)
        {
            return remote.GetPathInGitRepo(tfsPath);
        }

        private void Rename(IChange change, string pathInGitRepo, IGitTreeBuilder treeBuilder, ITfsWorkspace workspace, IGitTreeInformation initialTree)
        {
            var oldPath = GetPathInGitRepo(GetPathBeforeRename(change.Item), workspace.Remote);
            if (oldPath != null)
            {
                Delete(oldPath, treeBuilder, initialTree);
            }
            if (!change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
            {
                Update(change, pathInGitRepo, treeBuilder, workspace, initialTree);
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
                var previousChange = history.FirstOrDefault();
                if (previousChange == null)
                {
                    Trace.WriteLine(string.Format("No history found for item {0} changesetId {1}", item.ServerItem, item.ChangesetId));
                    return null;
                }
                oldItem = previousChange.Changes[0].Item;
            }
            return oldItem.ServerItem;
        }

        private void Update(IChange change, string pathInGitRepo, IGitTreeBuilder treeBuilder, ITfsWorkspace workspace, IGitTreeInformation initialTree)
        {
            if (change.Item.DeletionId == 0)
            {
                treeBuilder.Add(
                    pathInGitRepo,
                    workspace.GetLocalPath(pathInGitRepo),
                    GetMode(change, initialTree, pathInGitRepo)
                );
            }
        }

        public IEnumerable<TfsTreeEntry> GetTree()
        {
            return GetFullTree().Where(item => item.Item.ItemType == TfsItemType.File && !Summary.Remote.ShouldSkip(item.FullName));
        }

        public bool IsMergeChangeset
        {
            get
            {
                if (_changeset == null || _changeset.Changes == null || !_changeset.Changes.Any())
                    return false;
                return _changeset.Changes.Any(c => c.ChangeType.IncludesOneOf(TfsChangeType.Merge));
            }
        }

        public IEnumerable<TfsTreeEntry> GetFullTree()
        {
            IItem[] tfsItems;
            if(Summary.Remote.TfsRepositoryPath != null)
            {
                tfsItems = _changeset.VersionControlServer.GetItems(Summary.Remote.TfsRepositoryPath, _changeset.ChangesetId, TfsRecursionType.Full);   
            }
            else
            {
                tfsItems = Summary.Remote.TfsSubtreePaths.SelectMany(x => _changeset.VersionControlServer.GetItems(x, _changeset.ChangesetId, TfsRecursionType.Full)).ToArray();
            }
            var tfsItemsWithGitPaths = tfsItems.Select(item => new { item, gitPath = GetPathInGitRepo(item.ServerItem, this.Summary.Remote) });
            return tfsItemsWithGitPaths.Where(x => x.gitPath != null).Select(x => new TfsTreeEntry(x.gitPath, x.item));
        }

        public LogEntry CopyTree(IGitTreeBuilder treeBuilder, ITfsWorkspace workspace)
        {
            var startTime = DateTime.Now;
            var itemsCopied = 0;
            var maxChangesetId = 0;
            var tfsTreeEntries = GetTree().ToArray();
            if (tfsTreeEntries.Length == 0)
            {
                maxChangesetId = _changeset.ChangesetId;
            }
            else
            {
                workspace.Get(_changeset.ChangesetId);
                foreach (var entry in tfsTreeEntries)
                {
                    Add(entry.Item, entry.FullName, treeBuilder, workspace);
                    maxChangesetId = Math.Max(maxChangesetId, entry.Item.ChangesetId);

                    itemsCopied++;
                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(30))
                    {
                        _stdout.WriteLine("{0} objects created...", itemsCopied);
                        startTime = DateTime.Now;
                    }
                }
            }
            return MakeNewLogEntry(maxChangesetId == _changeset.ChangesetId ? _changeset : _tfs.GetChangeset(maxChangesetId));
        }

        private void Add(IItem item, string pathInGitRepo, IGitTreeBuilder treeBuilder, ITfsWorkspace workspace)
        {
            if (item.DeletionId == 0)
            {
                treeBuilder.Add(pathInGitRepo, workspace.GetLocalPath(pathInGitRepo), Mode.NewFile);
            }
        }

        private string GetMode(IChange change, IGitTreeInformation initialTree, string pathInGitRepo)
        {
            var existingMode = initialTree.GetMode(pathInGitRepo);
            if (existingMode != null &&
                !change.ChangeType.IncludesOneOf(TfsChangeType.Add))
            {
                return existingMode;
            }
            return Mode.NewFile;
        }

        private static readonly Regex SplitDirnameFilename = new Regex(@"(?<dir>.*)[/\\](?<file>[^/\\]+)");

        private void Delete(string pathInGitRepo, IGitTreeBuilder treeBuilder, IGitTreeInformation initialTree)
        {
            if (initialTree.GetMode(pathInGitRepo) != null)
            {
                treeBuilder.Remove(pathInGitRepo);
                Trace.WriteLine("\tD\t" + pathInGitRepo);
            }
        }

        private LogEntry MakeNewLogEntry()
        {
            return MakeNewLogEntry(_changeset, Summary.Remote);
        }

        private LogEntry MakeNewLogEntry(IChangeset changesetToLog, IGitTfsRemote remote = null)
        {
            var identity = _tfs.GetIdentity(changesetToLog.Committer);
            var name = changesetToLog.Committer;
            var email = changesetToLog.Committer;
            if (_authors != null && _authors.Authors.ContainsKey(changesetToLog.Committer))
            {
                name = _authors.Authors[changesetToLog.Committer].Name;
                email = _authors.Authors[changesetToLog.Committer].Email;
            }
            else if (identity != null)
            {
                //This can be null if the user was deleted from AD.
                //We want to keep their original history around with as little 
                //hassle to the end user as possible
                if (!String.IsNullOrWhiteSpace(identity.DisplayName))
                    name = identity.DisplayName;

                if (!String.IsNullOrWhiteSpace(identity.MailAddress))
                    email = identity.MailAddress;
            }
            else if (!String.IsNullOrWhiteSpace(changesetToLog.Committer))
            {
                string[] split = changesetToLog.Committer.Split('\\');
                if (split.Length == 2)
                {
                    name = split[1].ToLower();
                    email = string.Format("{0}@{1}.tfs.local", name, split[0].ToLower());
                }
            }

            // committer's & author's name and email MUST NOT be empty as otherwise they would be picked
            // by git from user.name and user.email config settings which is bad thing because commit could
            // be different depending on whose machine it fetched
            if (String.IsNullOrWhiteSpace(name))
            {
                name = "Unknown TFS user";
            }
            if (String.IsNullOrWhiteSpace(email))
            {
                email = "unknown@tfs.local";
            }
            return new LogEntry
                       {
                           Date = changesetToLog.CreationDate,
                           Log = changesetToLog.Comment + Environment.NewLine,
                           ChangesetId = changesetToLog.ChangesetId,
                           CommitterName = name,
                           AuthorName = name,
                           CommitterEmail = email,
                           AuthorEmail = email,
                           Remote = remote
                       };
        }
    }
}
