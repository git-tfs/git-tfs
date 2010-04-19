using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IItem
    {
        //return this.VersionControlServer.GetItem(item.ItemId, item.ChangesetId - 1)
        IItem GetVersion(int changeset);
        int ChangesetId { get; }
        string ServerItem { get; }
        decimal DeletionId { get; }
        TfsItemType ItemType { get; }
        void DownloadFile(string file);
    }
}