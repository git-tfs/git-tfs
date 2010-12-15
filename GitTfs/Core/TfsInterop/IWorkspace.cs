using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IWorkspace
    {
        IPendingChange[] GetPendingChanges();
        ICheckinEvaluationResult EvaluateCheckin(TfsCheckinEvaluationOptions options, IPendingChange[] allChanges, IPendingChange[] changes, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges);
        void Shelve(IShelveset shelveset, IPendingChange[] changes, TfsShelvingOptions options);
        int Checkin(IPendingChange[] changes, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges);
        int PendAdd(string path);
        int PendEdit(string path);
        int PendDelete(string path);
        int PendRename(string pathFrom, string pathTo);
        void ForceGetFile(string path, int changeset);
        string OwnerName { get; }
    }
}