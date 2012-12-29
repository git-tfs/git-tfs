using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface ITfsHelper
    {
        string TfsClientLibraryVersion { get; }
        string Url { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string[] LegacyUrls { get; set; }
        IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote);
        void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action);
        IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName);
        IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction);
        IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction);
        ICheckinNote CreateCheckinNote(Dictionary<string, string> checkinNotes);
        IIdentity GetIdentity(string username);
        ITfsChangeset GetLatestChangeset(GitTfsRemote remote);
        ITfsChangeset GetChangeset(int changesetId, GitTfsRemote remote);
        IChangeset GetChangeset(int changesetId);
        bool MatchesUrl(string tfsUrl);
        bool HasShelveset(string shelvesetName);
        ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName);
        int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote);
        bool CanShowCheckinDialog { get; }
        long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment);
        void CleanupWorkspaces(string workingDirectory);
        int GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null);
        IEnumerable<TfsLabel> GetLabels(string tfsPathBranch);
        bool CanGetBranchInformation { get; }
        IEnumerable<string> GetAllTfsBranchesOrderedByCreation();
        IBranch GetRootTfsBranchForRemotePath(string remoteTfsPath);
        void EnsureAuthenticated();
    }
}