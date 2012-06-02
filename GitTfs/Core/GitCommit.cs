using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp;

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
            var treesToDescend = new Queue<Tree>(new[] {_commit.Tree});
            while(treesToDescend.Any())
            {
                var currentTree = treesToDescend.Dequeue();
                foreach(var entry in currentTree)
                {
                    if(entry.Type == GitObjectType.Tree)
                    {
                        treesToDescend.Enqueue((Tree)entry.Target);
                    }
                    else if (entry.Type == GitObjectType.Blob)
                    {
                        yield return new GitTreeEntry(entry);
                    }
                    else
                    {
                        Trace.WriteLine("Not including " + entry.Name + ": type is " + entry.GetType().Name);
                    }
                }
            }
        }
    }
}