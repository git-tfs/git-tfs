using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using SEP.Extensions;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.VsCommon
{
	public partial class TfsHelper : ITfsHelper
	{
		private readonly TextWriter _stdout;
		private string _url;
		private readonly TfsApiBridge _bridge;

		public TfsHelper(TextWriter stdout, TfsApiBridge bridge)
		{
			_stdout = stdout;
			_bridge = bridge;
		}

		public string Url
		{
			get { return _url; }
			set { _url = value; UpdateServer(); }
		}

		private VersionControlServer VersionControl
		{
			get
			{
				var versionControlServer = (VersionControlServer)Server.GetService(typeof(VersionControlServer));
				versionControlServer.NonFatalError += NonFatalError;
				return versionControlServer;
			}
		}

		private WorkItemStore WorkItems
		{
			get
			{
				return (WorkItemStore)Server.GetService(typeof(WorkItemStore));
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
			get { return (IGroupSecurityService)Server.GetService(typeof(IGroupSecurityService)); }
		}

		public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote)
		{
			var changesets = VersionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
														 null, new ChangesetVersionSpec((int)startVersion), VersionSpec.Latest,
														 int.MaxValue, true,
														 true, true);
			try
			{
				if (changesets.Cast<Changeset>().Count() != 0) 
					return changesets.Cast<Changeset>().Select(changeset => BuildTfsChangeset(changeset, remote));
			}
			catch (Exception)
			{
				// no latest changeset - swallow the exception (ugly but intentional)
			}
			_stdout.WriteLine("No new changes in TFS");
			return new List<ITfsChangeset>();
		}

		private ITfsChangeset BuildTfsChangeset(Changeset changeset, GitTfsRemote remote)
		{
			return new TfsChangeset(this, _bridge.Wrap(changeset))
			{
				Summary = new TfsChangesetInfo { ChangesetId = changeset.ChangesetId, Remote = remote }
			};
		}

		public void WithWorkspace(string localDirectory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
		{
			var workspace = GetWorkspace(localDirectory, remote.TfsRepositoryPath);
			try
			{
				var tfsWorkspace = ObjectFactory.With("localDirectory").EqualTo(localDirectory)
					.With("remote").EqualTo(remote)
					.With("contextVersion").EqualTo(versionToFetch)
					.With("workspace").EqualTo(_bridge.Wrap(workspace))
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

		public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
		{
			return
				_bridge.Wrap(new Shelveset(_bridge.Unwrap(workspace).VersionControlServer, shelvesetName,
										   workspace.OwnerName));
		}

		public IIdentity GetIdentity(string username)
		{
			return _bridge.Wrap(GroupSecurityService.ReadIdentity(SearchFactor.AccountName, username, QueryMembership.None));
		}

		public ITfsChangeset GetLatestChangeset(GitTfsRemote remote)
		{
			var history = VersionControl.QueryHistory(remote.TfsRepositoryPath, VersionSpec.Latest, 0,
													  RecursionType.Full, null, null, VersionSpec.Latest, 1, true, false,
													  false);
			return BuildTfsChangeset(history.Cast<Changeset>().Single(), remote);
		}

		public IChangeset GetChangeset(int changesetId)
		{
			return _bridge.Wrap(VersionControl.GetChangeset(changesetId));
		}

		public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
		{
			return workItems.Select(workItem => _bridge.Wrap(GetWorkItemInfo(workItem, _bridge.Convert(checkinAction))));
		}

		private WorkItemCheckinInfo GetWorkItemInfo(string workItem, WorkItemCheckinAction checkinAction)
		{
			return new WorkItemCheckinInfo(WorkItems.GetWorkItem(Convert.ToInt32(workItem)), checkinAction);
		}
	}
}
