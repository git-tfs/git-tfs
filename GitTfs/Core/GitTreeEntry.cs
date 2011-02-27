using System.IO;
using GitSharp.Core;

namespace Sep.Git.Tfs.Core
{
    public class GitTreeEntry : ITreeEntry
    {
        private readonly FileTreeEntry _entry;

        public GitTreeEntry(FileTreeEntry entry)
        {
            _entry = entry;
        }

        public TreeEntry Entry { get { return _entry; } }

        public string FullName
        {
            get { return _entry.FullName; }
        }

        public Stream OpenRead()
        {
            return new MemoryStream(_entry.OpenReader().CachedBytes);
        }
    }
}