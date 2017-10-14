using System;
using System.IO;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class GitTreeEntry : ITreeEntry
    {
        private readonly TreeEntry _entry;

        public GitTreeEntry(TreeEntry entry)
        {
            _entry = entry;
        }

        public TreeEntry Entry { get { return _entry; } }

        public string FullName
        {
            get { return _entry.Path; }
        }

        public Stream OpenRead()
        {
            if (_entry.TargetType == TreeEntryTargetType.Blob)
            {
                return ((Blob)_entry.Target).GetContentStream();
            }
            throw new InvalidOperationException("Invalid object type");
        }
    }
}