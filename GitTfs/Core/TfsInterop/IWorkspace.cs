namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IWorkspace
    {
        IPendingChange[] GetPendingChanges();
        void Shelve(IShelveset shelveset, IPendingChange [] changes, TfsShelvingOptions options);
        int PendAdd(string path);
        int PendEdit(string path);
        int PendDelete(string path);
        void ForceGetFile(string path, int changeset);
        string OwnerName { get; }
    }
}