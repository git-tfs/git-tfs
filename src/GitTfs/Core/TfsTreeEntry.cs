using System.IO;
using GitTfs.Core.TfsInterop;
using GitTfs.Util;

namespace GitTfs.Core
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
        public string FullName { get { return _pathInGitRepo; } }
        public Stream OpenRead()
        {
            return new TemporaryFileStream(_item.DownloadFile());
        }
    }
}
