using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core
{
    public class TfsHelper : ITfsHelper
    {
        private TeamFoundationServer server;
        private string username;

        public string TfsClientLibraryVersion
        {
            // TODO -- fill this in.
            get { return "TODO"; }
        }

        public string Url
        {
            get { return server == null ? null : server.Uri.ToString(); }
            set { SetServer(value, Username); }
        }

        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                SetServer(Url, value);
            }
        }

        private void SetServer(string url, string username)
        {
            if(!string.IsNullOrEmpty(url))
            {
                server = null;
            }
            if(!string.IsNullOrEmpty(username))
            {
                throw new NotImplementedException("TODO: Using a non-default username is not yet supported.");
                //server = new TeamFoundationServer(url, new NetworkCredentials(username));
            }
            else
            {
                server = new TeamFoundationServer(url);
            }
        }

        private TeamFoundationServer Server
        {
            get
            {
                return server;
            }
        }
        private VersionControlServer VersionControl
        {
            get { return (VersionControlServer)Server.GetService(typeof(VersionControlServer)); }
        }

        public IEnumerable<TfsChangeset> GetChangesets(string basePath, long firstChangeset)
        {
            var changes = VersionControl.QueryHistory(basePath, VersionSpec.Latest, 0, RecursionType.Full,
                                        null, new ChangesetVersionSpec((int) firstChangeset), VersionSpec.Latest, 0, true,
                                        true, true);
            foreach(var change in changes)
            {
                System.Diagnostics.Trace.WriteLine("CHANGE: " + change.GetType());
            }
            throw new System.NotImplementedException();
        }
    }
}