using System;
using Microsoft.TeamFoundation.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public partial class TfsHelper : ITfsHelper
    {
        private TfsTeamProjectCollection server;

        public string TfsClientLibraryVersion
        {
            get { return typeof(TfsTeamProjectCollection).Assembly.GetName().Version.ToString() + " (MS)"; }
        }

        private void UpdateServer()
        {
            if (string.IsNullOrEmpty(Url))
            {
                server = null;
            }
            else
            {
                server = new TfsTeamProjectCollection(new Uri(Url), new UICredentialsProvider());
                server.EnsureAuthenticated();
            }
        }

        private TfsTeamProjectCollection Server
        {
            get
            {
                return server;
            }
        }
    }
}
