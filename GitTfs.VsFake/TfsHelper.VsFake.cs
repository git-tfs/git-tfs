using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsFake
{
    public class TfsHelper : ITfsHelper
    {
        public string TfsClientLibraryVersion { get { return "(FAKE)"; } }

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string[] LegacyUrls { get; set; }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            throw new NotImplementedException();
        }

        public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string username)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetLatestChangeset(GitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetChangeset(int changesetId, GitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public IChangeset GetChangeset(int changesetId)
        {
            throw new NotImplementedException();
        }

        public bool MatchesUrl(string tfsUrl)
        {
            throw new NotImplementedException();
        }

        public bool HasShelveset(string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public bool CanShowCheckinDialog { get { return false; } }

        public long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        public void CleanupWorkspaces(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        public void EnsureAuthenticated()
        {
            throw new NotImplementedException();
        }

    }
}
