using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using SEP.Extensions;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class TfsHelper : ITfsHelper
    {
        private TeamFoundationServer server;
        private string username;

        public string TfsClientLibraryVersion
        {
            get { return typeof(TeamFoundationServer).Assembly.GetName().Version.ToString() + " (MS)"; }
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
            if(string.IsNullOrEmpty(url))
            {
                server = null;
            }
            else
            {
                if(string.IsNullOrEmpty(username))
                {
                    server = new TeamFoundationServer(url);
                }
                else
                {
                    server = new TeamFoundationServer(url, MakeCredentials(username));
                }
            }
        }

        private ICredentials MakeCredentials(string username)
        {
            throw new NotImplementedException("TODO: Using a non-default username is not yet supported.");
        }

        private TeamFoundationServer Server
        {
            get
            {
                return server;
            }
        }

        public VersionControlServer VersionControl
        {
            get { return (VersionControlServer)Server.GetService(typeof(VersionControlServer)); }
        }

        private IGroupSecurityService GroupSecurityService
        {
            get { return (IGroupSecurityService) Server.GetService(typeof(IGroupSecurityService)); }
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion)
        {
            var changesets = VersionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
                                        null, new ChangesetVersionSpec((int) startVersion), VersionSpec.Latest, int.MaxValue, true,
                                        true, true);
            foreach (Changeset changeset in changesets)
            {
                yield return
                    new TfsChangeset(this, changeset)
                        {
                            Summary = new TfsChangesetInfo {ChangesetId = changeset.ChangesetId}
                        };
            }
        }

        public void WithWorkspace(string localDirectory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            var workspace = GetWorkspace(localDirectory, remote.TfsRepositoryPath);
            try
            {
                var tfsWorkspace = ObjectFactory.With("localDirectory").EqualTo(localDirectory)
                    .With("remote").EqualTo(remote)
                    .With("contextVersion").EqualTo(versionToFetch)
                    .With("workspace").EqualTo(workspace)
                    .GetInstance<TfsWorkspace>();
                action(tfsWorkspace);
            }
            finally
            {
                workspace.Delete();
            }
        }

        private Workspace GetWorkspace(string localDirectory, string repositoryPath)
        {
            var workspace = VersionControl.CreateWorkspace(GenerateWorkspaceName());
            workspace.CreateMapping(new WorkingFolder(repositoryPath, localDirectory));
            return workspace;
        }

        private string GenerateWorkspaceName()
        {
            return Guid.NewGuid().ToString();
        }

        public ITfsIdentity GetIdentity(string username)
        {
            return new TfsIdentity(GroupSecurityService.ReadIdentity(SearchFactor.AccountName, username, QueryMembership.None));
        }
    }
}
