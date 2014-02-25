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
        readonly PathResolver _resolver;
        readonly IEnumerable<NamedChange> _namedChanges;

        public ChangeSieve(IChangeset changeset, PathResolver resolver)
        {
            _resolver = resolver;

            _namedChanges = changeset.Changes.Select(c => new NamedChange
            {
                    Info = _resolver.GetGitObject(c.Item.ServerItem),
                    Change = c,
            });
        }

        bool? _renameBranchCommmit;
        /// <summary>
        /// Is the top-level folder deleted or renamed?
        /// </summary>
        private bool RenameBranchCommmit
        {
            get
            {
                if (!_renameBranchCommmit.HasValue)
                {
                    _renameBranchCommmit = NamedChanges.Any(c =>
                        c.Change.Item.ItemType == TfsItemType.Folder
                            && c.GitPath == string.Empty
                            && c.Change.ChangeType.IncludesOneOf(TfsChangeType.Delete, TfsChangeType.Rename));
        }
                return _renameBranchCommmit.Value;
            }
        }

        public IEnumerable<IChange> GetChangesToFetch()
        {
            if (DeletesProject)
                return Enumerable.Empty<IChange>();

            if (RenameBranchCommmit)
                return new List<IChange>();

            return NamedChanges.Where(c => IncludeInFetch(c)).Select(c => c.Change);
        }

        public IEnumerable<ApplicableChange> GetChangesToApply()
        {
            if (DeletesProject)
                return Enumerable.Empty<ApplicableChange>();

            if (RenameBranchCommmit)
                return new List<ApplicableChange>();

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
                    if (IncludeInApply(change))
                    {
                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath,
                            oldInfo != null ? oldInfo.Mode : Mode.NonExecutableFile));
                    }
                }
                else
                {
                    if (IncludeInApply(change))
                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath, change.Info.Mode));
                }
            }
            return compartments.Deleted.Concat(compartments.Updated);
        }

        bool? _deletesProject;
        private bool DeletesProject
        {
            get
            {
                if (!_deletesProject.HasValue)
                {
                    _deletesProject =
                        NamedChanges.Any(change =>
                            change.Change.Item.ItemType == TfsItemType.Folder
                               && change.GitPath == string.Empty
                               && change.Change.ChangeType.IncludesOneOf(TfsChangeType.Delete));
                }
                return _deletesProject.Value;
            }
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
                return _namedChanges;
            }
        }

        private bool IncludeInFetch(NamedChange change)
        {
            // If a change is only a branch operation and we already have a file at the target path,
            // then there is nothing to do for that change.
            if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Branch) &&
                !change.Change.ChangeType.IncludesOneOf(TfsChangeType.Edit) &&
                _resolver.Contains(change.GitPath))
            {
                return false;
            }

            return _resolver.ShouldIncludeGitItem(change.GitPath);
        }

        private bool IncludeInApply(NamedChange change)
        {
            return IncludeInFetch(change) && change.Change.Item.DeletionId == 0;
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
