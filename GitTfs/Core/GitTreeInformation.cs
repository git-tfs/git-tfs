using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class GitTreeInformation : IGitTreeInformation
    {
        private TreeDefinition _treeDefinition;

        public GitTreeInformation()
        {
            _treeDefinition = new TreeDefinition();
        }

        public GitTreeInformation(Tree tree)
        {
            _treeDefinition = TreeDefinition.From(tree);
        }

        public string GetMode(string path)
        {
            var entry = _treeDefinition[path];
            if (entry == null)
            {
                return null;
            }

            return entry.Mode.ToModeString();
        }
    }
}
