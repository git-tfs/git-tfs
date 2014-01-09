using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Util
{
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
            return FilteredChanges;
        }

        public IEnumerable<IChange> ChangesToApply()
        {
            return Sort(FilteredChanges);
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

        IEnumerable<IChange> _filteredChanges;
        private IEnumerable<IChange> FilteredChanges
        {
            get
            {
                if (_filteredChanges == null)
                {
                    _filteredChanges = _changeset.Changes.Where(change =>
                    {
                        var pathInGitRepo = GetPathInGitRepo(change.Item.ServerItem);
                        return (pathInGitRepo != null) && !_remote.ShouldSkip(pathInGitRepo);
                    });
                }
                return _filteredChanges;
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
    }
}
