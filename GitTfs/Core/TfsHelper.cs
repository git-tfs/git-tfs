using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly TextWriter _stdout;
        private TfsTeamProjectCollection server;
        private string username;

        public TfsHelper(TextWriter stdout)
        {
            _stdout = stdout;
        }

        public string TfsClientLibraryVersion
        {
            get { return typeof(TfsTeamProjectCollection).Assembly.GetName().Version.ToString() + " (MS)"; }
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

        public VersionControlServer VersionControl
        {
            get
            {
                var versionControlServer = (VersionControlServer)Server.GetService(typeof(VersionControlServer));
                versionControlServer.NonFatalError += NonFatalError;
                return versionControlServer;
            }
        }

        private void NonFatalError(object sender, ExceptionEventArgs e)
        {
            _stdout.WriteLine(e.Failure.Message);
            Trace.WriteLine("Failure: " + e.Failure.Inspect(), "tfs non-fatal error");
            Trace.WriteLine("Exception: " + e.Exception.Inspect(), "tfs non-fatal error");
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
