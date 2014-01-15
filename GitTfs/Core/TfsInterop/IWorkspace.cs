using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IWorkspace
    {
        IPendingChange[] GetPendingChanges();
        ICheckinEvaluationResult EvaluateCheckin(TfsCheckinEvaluationOptions options, IPendingChange[] allChanges, IPendingChange[] changes, string comment, string author, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges);
        void Shelve(IShelveset shelveset, IPendingChange[] changes, TfsShelvingOptions options);
        int Checkin(IPendingChange[] changes, string comment, string author, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges, TfsPolicyOverrideInfo policyOverrideInfo, bool overrideGatedCheckIn);
        int PendAdd(string path);
        int PendEdit(string path);
        int PendDelete(string path);
        int PendRename(string pathFrom, string pathTo);
        void ForceGetFile(string path, int changeset);
        void GetSpecificVersion(int changeset);
        void GetSpecificVersion(IChangeset changeset);
        void GetSpecificVersion(int changeset, IEnumerable<IChange> changes);
        string GetLocalItemForServerItem(string serverItem);
        string GetServerItemForLocalItem(string localItem);
        string OwnerName { get; }
        void Merge(string sourceTfsPath, string tfsRepositoryPath);
    }
}