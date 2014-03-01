using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Util
{
    public class PathResolver
    {
        readonly IGitTfsRemote _remote;
        readonly IDictionary<string, GitObject> _initialTree;

        public PathResolver(IGitTfsRemote remote, IDictionary<string, GitObject> initialTree)
        {
            _remote = remote;
            _initialTree = initialTree;
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            return GetGitObject(tfsPath).Try(x => x.Path);
        }

        public GitObject GetGitObject(string tfsPath)
        {
            var pathInGitRepo = _remote.GetPathInGitRepo(tfsPath);
            if (pathInGitRepo == null)
                return null;
            return Lookup(pathInGitRepo);
        }

        public bool ShouldIncludeGitItem(string gitPath)
        {
            return !String.IsNullOrEmpty(gitPath) && !_remote.ShouldSkip(gitPath);
        }

        public bool Contains(string pathInGitRepo)
        {
            if (pathInGitRepo != null && _initialTree.ContainsKey(pathInGitRepo))
                return true;
            return false;
        }

        private static readonly Regex SplitDirnameFilename = new Regex(@"(?<dir>.*)[/\\](?<file>[^/\\]+)");

        private GitObject Lookup(string pathInGitRepo)
        {
            GitObject gitObject;
            if (_initialTree.TryGetValue(pathInGitRepo, out gitObject))
                return gitObject;

            var fullPath = pathInGitRepo;
            var splitResult = SplitDirnameFilename.Match(pathInGitRepo);
            if (splitResult.Success)
            {
                var dirName = splitResult.Groups["dir"].Value;
                var fileName = splitResult.Groups["file"].Value;
                fullPath = Lookup(dirName).Path + "/" + fileName;
            }
            gitObject = new GitObject { Path = fullPath };
            _initialTree[fullPath] = gitObject;
            return gitObject;
        }
    }
}
