using LibGit2Sharp;

namespace GitTfs.Core
{
    public class GitTreeEntry : ITreeEntry
    {
        private readonly TreeEntry _entry;

        public GitTreeEntry(TreeEntry entry)
        {
            _entry = entry;
        }

        public TreeEntry Entry => _entry;

        public string FullName => _entry.Path;

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