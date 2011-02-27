using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitSharp.Core;

namespace Sep.Git.Tfs.Core
{
    public class GitCommit
    {
        private readonly Commit _commit;

        public GitCommit(Commit commit)
        {
            _commit = commit;
        }

        public IEnumerable<GitTreeEntry> GetTree()
        {
            var treesToDescend = new Queue<Tree>(new[] {_commit.TreeEntry});
            while(treesToDescend.Any())
            {
                var currentTree = treesToDescend.Dequeue();
                foreach(var entry in currentTree.Members)
                {
                    if(entry is Tree)
                    {
                        treesToDescend.Enqueue((Tree) entry);
                    }
                    else if (entry is FileTreeEntry)
                    {
                        yield return new GitTreeEntry((FileTreeEntry)entry);
                    }
                    else
                    {
                        Trace.WriteLine("Not including " + entry.FullName + ": type is " + entry.GetType().Name);
                    }
                }
            }
        }
    }
}