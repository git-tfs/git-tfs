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
        Stream DownloadFile();
    }

    public interface IItemDownloadStrategy
    {
        Stream DownloadFile(IItem item);
    }
}
