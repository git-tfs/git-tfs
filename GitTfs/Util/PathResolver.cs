﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Util
{
    public class PathResolver
    {
        IGitTfsRemote _remote;
        IDictionary<string, GitObject> _initialTree;

        public PathResolver(IGitTfsRemote remote, IDictionary<string, GitObject> initialTree)
        {
            _remote = remote;
            _initialTree = initialTree;
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            var pathInGitRepo = _remote.GetPathInGitRepo(tfsPath);
            if (pathInGitRepo == null)
                return null;
            return UpdateToMatchExtantCasing(pathInGitRepo);
        }

        public bool ShouldIncludeGitItem(string gitPath)
        {
            return !String.IsNullOrEmpty(gitPath) && !_remote.ShouldSkip(gitPath);
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