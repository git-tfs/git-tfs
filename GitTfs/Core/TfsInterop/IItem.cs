using System.IO;
namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IItem
    {
        IVersionControlServer VersionControlServer { get; }
        int ChangesetId { get; }
        string ServerItem { get; }
        decimal DeletionId { get; }
        TfsItemType ItemType { get; }
        int ItemId { get; }
        long ContentLength { get; }
        string DownloadFile();
    }

    public interface IItemDownloadStrategy
    {
        string DownloadFile(IItem item);
    }
}
