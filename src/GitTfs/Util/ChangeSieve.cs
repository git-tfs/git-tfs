using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using Mode = LibGit2Sharp.Mode;

namespace GitTfs.Util
{
    public enum ChangeType
    {
        Update,
        Delete,
        Ignore,
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

        public static ApplicableChange Ignore(string path)
        {
            return new ApplicableChange { Type = ChangeType.Ignore, GitPath = path };
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
        public IEnumerable<ApplicableChange> GetChangesToApply()
        {
            if (DeletesProject)
                return Enumerable.Empty<ApplicableChange>();

            var compartments = new
            {
                Deleted = new List<ApplicableChange>(),
                Updated = new List<ApplicableChange>(),
                Ignored = new List<ApplicableChange>(),
            };
            foreach (var change in NamedChanges)
            {
                // We only need the file changes because git only cares about files and if you make
                // changes to a folder in TFS, the changeset includes changes for all the descendant files anyway.
                if (change.Change.Item.ItemType != TfsItemType.File)
                    continue;

                if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                {
                    if (!IsGitPathMissing(change))
                        compartments.Deleted.Add(ApplicableChange.Delete(change.GitPath));
                }
                else
                {
                    var mode = change.Info != null ? change.Info.Mode : Mode.NonExecutableFile;

                    if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                    {
                        var oldInfo = _resolver.GetGitObject(GetPathBeforeRename(change.Change.Item));
                        if (oldInfo != null)
                        {
                            compartments.Deleted.Add(ApplicableChange.Delete(oldInfo.Path));

                            mode = oldInfo.Mode;
                        }
                    }

                    if (!IsItemDeleted(change)
                        && !IsGitPathMissing(change)
                        && !IsGitPathInDotGit(change)
                        && !IsIgnorable(change))
                    {
                        if (IsGitPathIgnored(change))
                            compartments.Ignored.Add(ApplicableChange.Ignore(change.GitPath));
                        else
                            compartments.Updated.Add(ApplicableChange.Update(change.GitPath, mode));
                    }
                }
            }
            return compartments.Deleted.Concat(compartments.Updated).Concat(compartments.Ignored);
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
            return !IsIgnorable(change)
                && !IsGitPathMissing(change)
                && !IsGitPathInDotGit(change)
                && !IsGitPathIgnored(change);
        }

        private bool IsIgnorable(NamedChange change)
        {
            return IgnorableChangeType(change.Change.ChangeType) && _resolver.Contains(change.GitPath);
         }

        private bool IgnorableChangeType(TfsChangeType changeType)
        {
            var isBranchOrMerge = (changeType & (TfsChangeType.Branch | TfsChangeType.Merge)) != 0;
            var isContentChange = (changeType & TfsChangeType.Content) != 0;
            return isBranchOrMerge && !isContentChange;
        }

        private bool IsGitPathMissing(NamedChange change)
        {
            return string.IsNullOrEmpty(change.GitPath);
        }

        private bool IsGitPathInDotGit(NamedChange change)
        {
            return IsInDotGit(change.GitPath);
        }

        private bool IsInDotGit(string path)
        {
            return _resolver.IsInDotGit(path);
        }

        private bool IsGitPathIgnored(NamedChange change)
        {
            return IsIgnored(change.GitPath);
        }

        private bool IsIgnored(string path)
        {
            return _resolver.IsIgnored(path);
        }

        private bool IsItemDeleted(NamedChange change)
        {
            return IsDeleted(change.Change.Item);
        }

        private bool IsDeleted(IItem item)
        {
            return item.DeletionId != 0;
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
