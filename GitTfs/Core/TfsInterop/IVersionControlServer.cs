namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IVersionControlServer
    {
        IItem GetItem(int itemId, int changesetNumber);
        IItem GetItem(string itemPath, int changsetNumber);
        IItem[] GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType);
    }
}