using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Mode = LibGit2Sharp.Mode;

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
        public Mode Mode { get; set; }

        public static ApplicableChange Update(string path, Mode mode = Mode.NonExecutableFile)
        {
            return new ApplicableChange { Type = ChangeType.Update, GitPath = path, Mode = mode };
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
                // We only need the file changes because git only cares about files and if you make
                // changes to a folder in TFS, the changeset includes changes for all the descendant files anyway.
                if (change.Change.Item.ItemType != TfsItemType.File)
                    continue;

                // If a change is only a branch operation and we already have a file at the target path,
                // then there is nothing to do for that change.
                if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Branch) &&
                    !change.Change.ChangeType.IncludesOneOf(TfsChangeType.Edit) &&
                    _resolver.Contains(change.GitPath))
                {
                    continue;
                }

                if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                {
                    if (change.GitPath != null)
                        compartments.Deleted.Add(ApplicableChange.Delete(change.GitPath));
                }
                else if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                {
                    var oldInfo = _resolver.GetGitObject(GetPathBeforeRename(change.Change.Item));
                    if (oldInfo != null)
                        compartments.Deleted.Add(ApplicableChange.Delete(oldInfo.Path));
                    if (Include(change))
                    {
                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath,
                            oldInfo.Try(x => x.Mode, () => Mode.NonExecutableFile)));
                    }
                }
                else
                {
                    if (Include(change))
                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath, change.Info.Mode));
                }
            }
            return compartments.Deleted.Concat(compartments.Updated);
        }

        class NamedChange
        {
            public GitObject Info { get; set; }
            public IChange Change { get; set; }
            public string GitPath { get { return Info.Try(x => x.Path); } }
        }

        IEnumerable<NamedChange> NamedChanges
        {
            get
            {
                return _changeset.Changes.Select(c => new NamedChange {
                    Info = _resolver.GetGitObject(c.Item.ServerItem),
                    Change = c,
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
