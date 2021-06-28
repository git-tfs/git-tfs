using System;
using System.Collections.Generic;
using GitTfs.Commands;

namespace GitTfs.Core.TfsInterop
{
    public interface ITfsHelper
    {
        string TfsClientLibraryVersion { get; }
        string Url { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        IEnumerable<ITfsChangeset> GetChangesets(string path, int startVersion, IGitTfsRemote remote, int lastVersion = -1, bool byLots = false);
        void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action);
        IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName);
        IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction);
        IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction);
        ICheckinNote CreateCheckinNote(Dictionary<string, string> checkinNotes);
        IIdentity GetIdentity(string username);
        ITfsChangeset GetLatestChangeset(IGitTfsRemote remote);
        int GetLatestChangesetId(IGitTfsRemote remote);
        ITfsChangeset GetChangeset(int changesetId, IGitTfsRemote remote);
        IChangeset GetChangeset(int changesetId);
        bool HasShelveset(string shelvesetName);
        ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName);
        int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote);
        bool CanShowCheckinDialog { get; }
        int ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment);
        void CleanupWorkspaces(string workingDirectory);
        IList<RootBranch> GetRootChangesetForBranch(string tfsPathBranchToCreate, int lastChangesetIdToCheck = -1, string tfsPathParentBranch = null);
        IEnumerable<TfsLabel> GetLabels(string tfsPathBranch, string nameFilter = null);
        IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation();
        IEnumerable<IBranchObject> GetBranches(bool getDeletedBranches = false);
        void EnsureAuthenticated();
        void CreateBranch(string sourcePath, string targetPath, int changesetId, string comment = null);
        void CreateTfsRootBranch(string projectName, string mainBranch, string gitRepositoryPath, bool createTeamProjectFolder);
        bool IsExistingInTfs(string path);
        int FindMergeChangesetParent(string path, int firstChangeset, GitTfsRemote remote);
        /// <summary>
        /// Creates and maps a workspace for the given remote with the given local -> server directory mappings, at the given Tfs version,
        /// and then performs the action.
        /// </summary>
        /// <param name="localDirectory">The local base directory containing all the mappings</param>
        /// <param name="remote">The owning remote</param>
        /// <param name="mappings">The workspace mappings to create.  Item1 is the relative path from the localDirectory, and Item2 is the TfsRepositoryPath</param>
        /// <param name="versionToFetch">The TFS version to fetch from the server</param>
        /// <param name="action">The action to perform</param>
        void WithWorkspace(string localDirectory, IGitTfsRemote remote, IEnumerable<Tuple<string, string>> mappings, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action);
        int QueueGatedCheckinBuild(Uri value, string buildDefinitionName, string shelvesetName, string checkInTicket);
        void DeleteShelveset(IWorkspace workspace, string shelvesetName);
    }
}
