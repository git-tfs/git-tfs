using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public partial class TfsHelper : ITfsHelper
    {
        private TeamFoundationServer server;

        public string TfsClientLibraryVersion
        {
            get { return typeof(TeamFoundationServer).Assembly.GetName().Version.ToString() + " (MS)"; }
        }

        public bool CanShowCheckinDialog { get { return false; } }

        public long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        private void UpdateServer()
        {
            if (string.IsNullOrEmpty(Url))
            {
                server = null;
            }
            else
            {
                server = new TeamFoundationServer(Url, new UICredentialsProvider());
                server.EnsureAuthenticated();
            }
        }

        private TeamFoundationServer Server
        {
            get
            {
                return server;
            }
        }
    }
}