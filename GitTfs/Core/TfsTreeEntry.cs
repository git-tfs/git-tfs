using System.IO;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Core
{
    public class TfsTreeEntry : ITreeEntry
    {
        private readonly string _pathInGitRepo;
        private readonly IItem _item;

        public TfsTreeEntry(string pathInGitRepo, IItem item)
        {
            _pathInGitRepo = pathInGitRepo;
            _item = item;
        }

        public IItem Item { get { return _item; } }
        public string FullName { get { return _pathInGitRepo; } } // WARNING: this will no longer be the case-corrected path
        public Stream OpenRead()
        {
            return new TemporaryFileStream(_item.DownloadFile());
        }
    }
}
