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
        private readonly PathResolver _resolver;
        private readonly IEnumerable<NamedChange> _namedChanges;

        public ChangeSieve(IChangeset changeset, PathResolver resolver)
        {
            _resolver = resolver;

            _namedChanges = changeset.Changes.Select(c => new NamedChange
            {
                Info = _resolver.GetGitObject(c.Item.ServerItem),
                Change = c,
            });
        }

        private bool? _renameBranchCommmit;
        /// <summary>
        /// Is the top-level folder deleted or renamed?
        /// </summary>
        public bool RenameBranchCommmit
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

            return NamedChanges.Where(c => IncludeInFetch(c)).Select(c => c.Change);
        }

        /// <summary>
        /// Get all the changes of a changeset to apply
        /// </summary>
        /// <param name="forceGetChanges">true - force get changes ignoring check what should be applied. </param>
        public IEnumerable<ApplicableChange> GetChangesToApply(bool forceGetChanges = false)
        {
            if (DeletesProject)
                return Enumerable.Empty<ApplicableChange>();

            var compartments = new
            {
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
                    if (forceGetChanges || IncludeInApply(change))
                    {
                        // for get changes only on first change set
                        forceGetChanges = false;

                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath, change.Info.Mode));
                    }
                }
            }
            return compartments.Deleted.Concat(compartments.Updated);
        }

        private bool? _deletesProject;
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

        private class NamedChange
        {
            public GitObject Info { get; set; }
            public IChange Change { get; set; }
            public string GitPath { get { return Info.Try(x => x.Path); } }
        }

        private IEnumerable<NamedChange> NamedChanges
        {
            get
            {
                return _namedChanges;
            }
        }

        private bool IncludeInFetch(NamedChange change)
        {
            if (IgnorableChangeType(change.Change.ChangeType) && _resolver.Contains(change.GitPath))
            {
                return false;
            }

            return _resolver.ShouldIncludeGitItem(change.GitPath);
        }

        private bool IgnorableChangeType(TfsChangeType changeType)
        {
            var isBranchOrMerge = (changeType & (TfsChangeType.Branch | TfsChangeType.Merge)) != 0;
            var isContentChange = (changeType & TfsChangeType.Content) != 0;
            return isBranchOrMerge && !isContentChange;
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
                try
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
                catch
                {
                    Trace.WriteLine(string.Format("No history found for item {0} changesetId {1}", item.ServerItem, item.ChangesetId));
                    return null;
                }
            }
            return oldItem.ServerItem;
        }
    }
}
