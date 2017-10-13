using System;
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
            var treesToDescend = new Queue<Tree>(new[] { _commit.Tree });
            while (treesToDescend.Any())
            {
                var currentTree = treesToDescend.Dequeue();
                foreach (var entry in currentTree)
                {
                    if (entry.TargetType == TreeEntryTargetType.Tree)
                    {
                        treesToDescend.Enqueue((Tree)entry.Target);
                    }
                    else if (entry.TargetType == TreeEntryTargetType.Blob)
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

        public Tuple<string, string> AuthorAndEmail
        {
            get
            {
                return new Tuple<string, string>(_commit.Author.Name, _commit.Author.Email);
            }
        }

        public DateTimeOffset When
        {
            get
            {
                return _commit.Author.When;
            }
        }

        public string Sha
        {
            get
            {
                return _commit.Sha;
            }
        }

        public string Message
        {
            get
            {
                return _commit.Message;
            }
        }

        public IEnumerable<GitCommit> Parents
        {
            get { return _commit.Parents.Select(c => new GitCommit(c)); }
        }
    }
}

