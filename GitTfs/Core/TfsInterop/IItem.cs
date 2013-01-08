using System.IO;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Core.TfsInterop
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

    public interface IItemIdentifier
    {
        
    }

    public interface IItemDownloadStrategy
    {
        TemporaryFile DownloadFile(IItem item);
    }
}
