using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class GitTreeBuilder : IGitTreeBuilder
    {
        private TreeDefinition _treeDefinition;
        private ObjectDatabase _objectDatabase;

        public GitTreeBuilder(ObjectDatabase objectDatabase)
        {
            _treeDefinition = new TreeDefinition();
            _objectDatabase = objectDatabase;
        }

        public GitTreeBuilder(ObjectDatabase objectDatabase, Tree tree)
        {
            _treeDefinition = TreeDefinition.From(tree);
            _objectDatabase = objectDatabase;
        }

        public void Add(string path, string file, LibGit2Sharp.Mode mode)
        {
            _treeDefinition.Add(path, file, mode);
        }

        public void Remove(string path)
        {
            _treeDefinition.Remove(path);
        }

        public string GetTree()
        {
            return _objectDatabase.CreateTree(_treeDefinition).Sha;
        }
    }
}
