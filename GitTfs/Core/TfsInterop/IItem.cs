namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IItem
    {
        IItem GetVersion(int changeset);
        int ChangesetId { get; }
        string ServerItem { get; }
        decimal DeletionId { get; }
        TfsItemType ItemType { get; }
        void DownloadFile(string file);
    }
}