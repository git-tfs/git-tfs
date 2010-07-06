using System.Net;
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

        private void SetServer(string url, string username)
        {
            if (string.IsNullOrEmpty(url))
            {
                server = null;
            }
            else
            {
                if (string.IsNullOrEmpty(username))
                {
                    server = new TeamFoundationServer(url);
                }
                else
                {
                    server = new TeamFoundationServer(url, new UICredentialsProvider());
					server.EnsureAuthenticated();
                }
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