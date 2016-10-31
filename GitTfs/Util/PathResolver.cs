using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Util
{
    public class PathResolver
    {
        private readonly IGitTfsRemote _remote;
        private readonly string _relativePath;
        private readonly IDictionary<string, GitObject> _initialTree;

        public PathResolver(IGitTfsRemote remote, string relativePath, IDictionary<string, GitObject> initialTree)
        {
            _remote = remote;
            _relativePath = relativePath;
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
            if (!string.IsNullOrEmpty(_relativePath))
                pathInGitRepo = _relativePath + "/" + pathInGitRepo;
            return Lookup(pathInGitRepo);
        }

        public bool ShouldIncludeGitItem(string gitPath)
        {
            return !string.IsNullOrEmpty(gitPath) && !_remote.ShouldSkip(gitPath);
        }

        public bool Contains(string pathInGitRepo)
        {
            if (pathInGitRepo != null)
            {
                GitObject result;
                if (_initialTree.TryGetValue(pathInGitRepo, out result))
                    return result.Commit != null;
            }
            return false;
        }

        private static readonly Regex SplitDirnameFilename = new Regex(@"(?<dir>.*)[/\\](?<file>[^/\\]+)", RegexOptions.Compiled);

        private GitObject Lookup(string pathInGitRepo)
        {
            GitObject result;
            if (_initialTree.TryGetValue(pathInGitRepo, out result))
                return result;

            var fullPath = pathInGitRepo;
            var splitResult = SplitDirnameFilename.Match(pathInGitRepo);
            if (splitResult.Success)
            {
                var dirName = splitResult.Groups["dir"].Value;
                var fileName = splitResult.Groups["file"].Value;
                fullPath = Lookup(dirName).Path + "/" + fileName;
            }
            result = new GitObject { Path = fullPath };
            _initialTree[fullPath] = result;
            return result;
        }
    }
}
