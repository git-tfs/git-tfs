using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Util
{
    public enum ChangeType
    {
        Update,
        Delete,
    }

    public class ApplicableChange
    {
        public ChangeType Type { get; set; }
        public string GitPath { get; set; }

        public static ApplicableChange Update(string path)
        {
            return new ApplicableChange { Type = ChangeType.Update, GitPath = path };
        }

        public static ApplicableChange Delete(string path)
        {
            return new ApplicableChange { Type = ChangeType.Delete, GitPath = path };
        }
    }

    public class ChangeSieve
    {
        readonly IChangeset _changeset;
        readonly PathResolver _resolver;

        public ChangeSieve(IChangeset changeset, PathResolver resolver)
        {
            _changeset = changeset;
            _resolver = resolver;
        }

        public IEnumerable<IChange> GetChangesToFetch()
        {
            return NamedChanges.Where(c => Include(c.GitPath)).Select(c => c.Change);
        }

        public IEnumerable<ApplicableChange> GetChangesToApply()
        {
            var compartments = new {
                Deleted = new List<ApplicableChange>(),
                Updated = new List<ApplicableChange>(),
            };
            foreach (var change in NamedChanges)
            {
                if (change.Change.Item.ItemType == TfsItemType.File)
                {
                    if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                    {
                        if (change.GitPath != null)
                            compartments.Deleted.Add(ApplicableChange.Delete(change.GitPath));
                    }
                    else if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                    {
                        var oldPath = _resolver.GetPathInGitRepo(GetPathBeforeRename(change.Change.Item));
                        if (oldPath != null)
                            compartments.Deleted.Add(ApplicableChange.Delete(oldPath));
                        if (Include(change))
                            compartments.Updated.Add(ApplicableChange.Update(change.GitPath));
                    }
                    else
                    {
                        if (Include(change))
                            compartments.Updated.Add(ApplicableChange.Update(change.GitPath));
                    }
                }
            }
            return compartments.Deleted.Concat(compartments.Updated);
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

        class NamedChange
        {
            public string GitPath { get; set; }
            public IChange Change { get; set; }
        }

        IEnumerable<NamedChange> NamedChanges
        {
            get
            {
                return _changeset.Changes.Select(c => new NamedChange {
                    GitPath = _resolver.GetPathInGitRepo(c.Item.ServerItem),
                    Change = c
                });
            }
        }

        private bool Include(NamedChange change)
        {
            return Include(change.GitPath) && change.Change.Item.DeletionId == 0;
        }

        private bool Include(string pathInGitRepo)
        {
            return _resolver.ShouldIncludeGitItem(pathInGitRepo);
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
    }
}
