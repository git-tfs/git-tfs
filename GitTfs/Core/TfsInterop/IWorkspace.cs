using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IWorkspace
    {
        IEnumerable<IPendingChange> GetPendingChanges();
        void Shelve(IShelveset shelveset, IEnumerable<IPendingChange> changes, TfsShelvingOptions options);
        int PendAdd(string path);
        int PendEdit(string path);
        int PendDelete(string path);
        void ForceGetFile(string path, int changeset);
        string OwnerName { get; }
    }
}