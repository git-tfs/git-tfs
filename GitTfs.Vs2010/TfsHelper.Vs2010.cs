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

        private void SetServer(string url, string username)
        {
            if(string.IsNullOrEmpty(url))
            {
                server = null;
            }
            else
            {
                if(string.IsNullOrEmpty(username))
                {
                    server = new TfsTeamProjectCollection(new Uri(url));
                }
                else
                {
                    server = new TfsTeamProjectCollection(new Uri(url), new UICredentialsProvider());
					server.EnsureAuthenticated();
                }
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