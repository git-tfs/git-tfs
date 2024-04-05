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

        public IItem Item => _item;
        public string FullName => _pathInGitRepo;
        public Stream OpenRead() => new TemporaryFileStream(_item.DownloadFile());
    }
}
