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
        readonly IDictionary<string, GitObject> _initialTree;
        readonly IChangeset _changeset;
        readonly IGitTfsRemote _remote;

        public ChangeSieve(IDictionary<string, GitObject> initialTree, IChangeset changeset, IGitTfsRemote remote)
        {
            _initialTree = initialTree;
            _changeset = changeset;
            _remote = remote;
        }

        public bool HasChanges()
        {
            return FilteredChanges.Any();
        }

        public IEnumerable<IChange> ChangesToFetch()
        {
            return FilteredChanges.Select(c => c.Change);
        }

        public IEnumerable<IChange> ChangesToApply()
        {
            return Sort(ChangesToFetch());
        }

        public IEnumerable<ApplicableChange> ChangesToApply2()
        {
            var compartments = new {
                Deleted = new List<ApplicableChange>(),
                Updated = new List<ApplicableChange>(),
            };
            foreach (var change in NamedChanges)
            {
                if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                {
                    if(change.GitPath != null)
                        compartments.Deleted.Add(ApplicableChange.Delete(change.GitPath));
                }
                else if (change.Change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                {
                    var oldPath = GetPathInGitRepo(GetPathBeforeRename(change.Change.Item));
                    if (oldPath != null)
                        compartments.Deleted.Add(ApplicableChange.Delete(oldPath));
                    if (change.GitPath != null && !_remote.ShouldSkip(change.GitPath))
                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath));
                }
                else
                {
                    if (change.GitPath != null && !_remote.ShouldSkip(change.GitPath))
                        compartments.Updated.Add(ApplicableChange.Update(change.GitPath));
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
                return _changeset.Changes.Select(c => new NamedChange { GitPath = GetPathInGitRepo(c.Item.ServerItem), Change = c });
            }
        }

        IEnumerable<NamedChange> FilteredChanges
        {
            get
            {
                return NamedChanges.Where(c => c.GitPath != null && !_remote.ShouldSkip(c.GitPath));
            }
        }

        private string GetPathInGitRepo(string tfsPath)
        {
            var pathInGitRepo = _remote.GetPathInGitRepo(tfsPath);
            if (pathInGitRepo == null)
                return null;
            return UpdateToMatchExtantCasing(pathInGitRepo);
        }

        private static readonly Regex SplitDirnameFilename = new Regex(@"(?<dir>.*)[/\\](?<file>[^/\\]+)");

        private string UpdateToMatchExtantCasing(string pathInGitRepo)
        {
            if (_initialTree.ContainsKey(pathInGitRepo))
                return _initialTree[pathInGitRepo].Path;

            var fullPath = pathInGitRepo;
            var splitResult = SplitDirnameFilename.Match(pathInGitRepo);
            if (splitResult.Success)
            {

                var dirName = splitResult.Groups["dir"].Value;
                var fileName = splitResult.Groups["file"].Value;
                fullPath = UpdateToMatchExtantCasing(dirName) + "/" + fileName;
            }
            _initialTree[fullPath] = new GitObject { Path = fullPath };
            return fullPath;
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
