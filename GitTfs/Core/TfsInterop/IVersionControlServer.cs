namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IVersionControlServer
    {
        IItem GetItem(int itemId, int changesetNumber);
    }
}