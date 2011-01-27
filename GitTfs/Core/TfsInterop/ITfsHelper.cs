using System;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface ITfsHelper
    {
        string TfsClientLibraryVersion { get; }
        string Url { get; set; }
        string[] LegacyUrls { get; set; }
        IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote);
        void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action);
        IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName);
        IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction);
        IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction);
        IIdentity GetIdentity(string username);
        ITfsChangeset GetLatestChangeset(GitTfsRemote remote);
        IChangeset GetChangeset(int changesetId);
        bool MatchesUrl(string tfsUrl);
        bool HasShelveset(string shelvesetName);
        bool CanShowCheckinDialog { get; }
        long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment);
        void CleanupWorkspaces(string workingDirectory);
    }
}