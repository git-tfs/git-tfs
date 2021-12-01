using GitTfs.Util;

namespace GitTfs.Core.TfsInterop
{
    public interface IItem
    {
        IVersionControlServer VersionControlServer { get; }
        int ChangesetId { get; }
        string ServerItem { get; }
        int DeletionId { get; }
        TfsItemType ItemType { get; }
        int ItemId { get; }
        long ContentLength { get; }
        TemporaryFile DownloadFile();
    }
}
