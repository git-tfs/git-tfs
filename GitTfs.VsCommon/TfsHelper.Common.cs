using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using SEP.Extensions;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;
using ChangeType = Microsoft.TeamFoundation.Server.ChangeType;

namespace Sep.Git.Tfs.VsCommon
{
    public abstract class TfsHelperBase : ITfsHelper
    {
        private readonly TextWriter _stdout;
        private readonly TfsApiBridge _bridge;
        private readonly IContainer _container;

        public TfsHelperBase(TextWriter stdout, TfsApiBridge bridge, IContainer container)
        {
            _stdout = stdout;
            _bridge = bridge;
            _container = container;
        }

        public abstract string TfsClientLibraryVersion { get; }

        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool HasCredentials
        {
            get { return !String.IsNullOrEmpty(Username); }
        }

        public abstract void EnsureAuthenticated();

        private string[] _legacyUrls;


        public string[] LegacyUrls
        {
            get { return _legacyUrls ?? (_legacyUrls = new string[0]); }
            set { _legacyUrls = value; }
        }

        protected NetworkCredential GetCredential()
        {
            var idx = Username.IndexOf('\\');
            if (idx >= 0)
            {
                string domain = Username.Substring(0, idx);
                string login = Username.Substring(idx + 1);
                return new NetworkCredential(login, Password, domain);
            }
            return new NetworkCredential(Username, Password);
        }

        protected abstract T GetService<T>();

        protected VersionControlServer VersionControl
        {
            get
            {
                var versionControlServer = GetService<VersionControlServer>();
                versionControlServer.NonFatalError += NonFatalError;
                return versionControlServer;
            }
        }

        private WorkItemStore WorkItems
        {
            get { return GetService<WorkItemStore>(); }
        }

        private void NonFatalError(object sender, ExceptionEventArgs e)
        {
            _stdout.WriteLine(e.Failure.Message);
            Trace.WriteLine("Failure: " + e.Failure.Inspect(), "tfs non-fatal error");
            Trace.WriteLine("Exception: " + e.Exception.Inspect(), "tfs non-fatal error");
        }

        private IGroupSecurityService GroupSecurityService
        {
            get { return GetService<IGroupSecurityService>(); }
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote)
        {
            var changesets = VersionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
                                                         null, new ChangesetVersionSpec((int) startVersion), VersionSpec.Latest, int.MaxValue, true,
                                                         true, true);
            return changesets.Cast<Changeset>()
                .OrderBy(changeset => changeset.ChangesetId)
                .Select(changeset => BuildTfsChangeset(changeset, remote));
        }

        private ITfsChangeset BuildTfsChangeset(Changeset changeset, GitTfsRemote remote)
        {
            var tfsChangeset = _container.With<ITfsHelper>(this).With<IChangeset>(_bridge.Wrap<WrapperForChangeset, Changeset>(changeset)).GetInstance<TfsChangeset>();
            tfsChangeset.Summary = new TfsChangesetInfo {ChangesetId = changeset.ChangesetId, Remote = remote};
            return tfsChangeset;
        }

        public void WithWorkspace(string localDirectory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            var workspace = GetWorkspace(localDirectory, remote.TfsRepositoryPath);
            try
            {
                var tfsWorkspace = _container.With("localDirectory").EqualTo(localDirectory)
                    .With("remote").EqualTo(remote)
                    .With("contextVersion").EqualTo(versionToFetch)
                    .With("workspace").EqualTo(_bridge.Wrap<WrapperForWorkspace, Workspace>(workspace))
                    .With("tfsHelper").EqualTo(this)
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
            try
            {
                var workspace = VersionControl.CreateWorkspace(GenerateWorkspaceName());
                workspace.CreateMapping(new WorkingFolder(repositoryPath, localDirectory));
                return workspace;
            }
            catch (MappingConflictException e)
            {
                throw new GitTfsException(e.Message, new[] {"Run 'git tfs cleanup-workspaces' to remove the workspace."}, e);
            }
        }

        private string GenerateWorkspaceName()
        {
            return "git-tfs-" + Guid.NewGuid();
        }

        public abstract long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment);

        public void CleanupWorkspaces(string workingDirectory)
        {
            Trace.WriteLine("Looking for workspaces mapped to @\"" + workingDirectory + "\"...", "cleanup-workspaces");
            var workspace = VersionControl.TryGetWorkspace(workingDirectory);
            if (workspace != null)
            {
                Trace.WriteLine("Found mapping in workspace \"" + workspace.DisplayName + "\".", "cleanup-workspaces");
                if (workspace.Folders.Length == 1)
                {
                    _stdout.WriteLine("Removing workspace \"" + workspace.DisplayName + "\".");
                    workspace.Delete();
                }
                else
                {
                    foreach (var mapping in workspace.Folders.Where(f => Path.GetFullPath(f.LocalItem).ToLower() == Path.GetFullPath(workingDirectory).ToLower()))
                    {
                        _stdout.WriteLine("Removing @\"" + mapping.LocalItem + "\" from workspace \"" + workspace.DisplayName + "\".");
                        workspace.DeleteMapping(mapping);
                    }
                }
            }
        }

        public bool HasShelveset(string shelvesetName)
        {
            var matchingShelvesets = VersionControl.QueryShelvesets(shelvesetName, GetAuthenticatedUser());
            return matchingShelvesets != null && matchingShelvesets.Length > 0;
        }

        protected abstract string GetAuthenticatedUser();

        public abstract bool CanShowCheckinDialog { get; }

        public ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName)
        {
            shelvesetOwner = shelvesetOwner == "all" ? null : (shelvesetOwner ?? GetAuthenticatedUser());
            var shelvesets = VersionControl.QueryShelvesets(shelvesetName, shelvesetOwner);
            if (shelvesets.Length != 1)
            {
                throw new GitTfsException("Unable to find " + shelvesetOwner + "'s shelveset \"" + shelvesetName + "\" (" + shelvesets.Length + " matches).")
                    .WithRecommendation("Try providing the shelveset owner.");
            }
            var shelveset = shelvesets.First();

            var change = VersionControl.QueryShelvedChanges(shelveset).Single();
            var wrapperForVersionControlServer =
                _bridge.Wrap<WrapperForVersionControlServer, VersionControlServer>(VersionControl);
            // TODO - containerify this (no `new`)!
            var fakeChangeset = new FakeChangeset(shelveset, change, wrapperForVersionControlServer, _bridge);
            var tfsChangeset = new TfsChangeset(remote.Tfs, fakeChangeset, _stdout, null) { Summary = new TfsChangesetInfo { Remote = remote } };
            return tfsChangeset;
        }

        public int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote)
        {
            var shelvesetOwner = shelveList.Owner == "all" ? null : (shelveList.Owner ?? GetAuthenticatedUser());
            IEnumerable<Shelveset> shelvesets;
            try
            {
                shelvesets = VersionControl.QueryShelvesets(null, shelvesetOwner);
            }
            catch(IdentityNotFoundException e)
            {
                _stdout.WriteLine("User '{0}' not found", shelveList.Owner);
                return GitTfsExitCodes.InvalidArguments;
            }
            if (shelvesets.Empty())
            {
                _stdout.WriteLine("No changesets found.");
                return GitTfsExitCodes.OK;
            }

            string sortBy = shelveList.SortBy;
            if (sortBy != null)
            {
                switch (sortBy.ToLowerInvariant())
                {
                    case "date":
                        shelvesets = shelvesets.OrderBy(s => s.CreationDate);
                        break;
                    case "owner":
                        shelvesets = shelvesets.OrderBy(s => s.OwnerName);
                        break;
                    case "name":
                        shelvesets = shelvesets.OrderBy(s => s.Name);
                        break;
                    case "comment":
                        shelvesets = shelvesets.OrderBy(s => s.Comment);
                        break;
                    default:
                        _stdout.WriteLine("ERROR: sorting criteria '{0}' is invalid. Possible values are: date, owner, name, comment", sortBy);
                        return GitTfsExitCodes.InvalidArguments;
                }
            }
            if (shelveList.FullFormat)
                WriteShelvesetsToStdoutDetailed(shelvesets);
            else
                WriteShelvesetsToStdout(shelvesets);
            return GitTfsExitCodes.OK;
        }

        private void WriteShelvesetsToStdout(IEnumerable<Shelveset> shelvesets)
        {
            foreach (var shelveset in shelvesets)
            {
                _stdout.WriteLine("  {0,-20} {1,-20}", shelveset.OwnerName, shelveset.Name);
            }
        }

        private void WriteShelvesetsToStdoutDetailed(IEnumerable<Shelveset> shelvesets)
        {
            foreach (var shelveset in shelvesets)
            {
                _stdout.WriteLine("Name   : {0}", shelveset.Name);
                _stdout.WriteLine("Owner  : {0}", shelveset.OwnerName);
                _stdout.WriteLine("Date   : {0:g}", shelveset.CreationDate);
                _stdout.WriteLine("Comment: {0}", shelveset.Comment);
                _stdout.WriteLine();
            }
        }

        #region Fake classes for unshelve

        private class FakeChangeset : IChangeset
        {
            private readonly Shelveset _shelveset;
            private readonly PendingSet _pendingSet;
            private readonly IVersionControlServer _versionControlServer;
            private readonly TfsApiBridge _bridge;
            private readonly IChange[] _changes;

            public FakeChangeset(Shelveset shelveset, PendingSet pendingSet, IVersionControlServer versionControlServer, TfsApiBridge bridge)
            {
                _shelveset = shelveset;
                _versionControlServer = versionControlServer;
                _bridge = bridge;
                _pendingSet = pendingSet;
                _changes = _pendingSet.PendingChanges.Select(x => new FakeChange(x, _bridge, versionControlServer)).Cast<IChange>().ToArray();
            }

            public IChange[] Changes
            {
                get { return _changes; }
            }

            public string Committer
            {
                get { return _pendingSet.OwnerName; }
            }

            public DateTime CreationDate
            {
                get { return _shelveset.CreationDate; }
            }

            public string Comment
            {
                get { return _shelveset.Comment; }
            }

            public int ChangesetId
            {
                get { return -1; }
            }

            public IVersionControlServer VersionControlServer
            {
                get { return _versionControlServer; }
            }
        }

        private class FakeChange : IChange
        {
            private readonly PendingChange _pendingChange;
            private readonly TfsApiBridge _bridge;
            private readonly FakeItem _fakeItem;

            public FakeChange(PendingChange pendingChange, TfsApiBridge bridge, IVersionControlServer versionControlServer)
            {
                _pendingChange = pendingChange;
                _bridge = bridge;
                _fakeItem = new FakeItem(_pendingChange, _bridge, versionControlServer);
            }

            public TfsChangeType ChangeType
            {
                get { return _bridge.Convert<TfsChangeType>(_pendingChange.ChangeType); }
            }

            public IItem Item
            {
                get { return _fakeItem; }
            }
        }

        private class FakeItem : IItem
        {
            private readonly PendingChange _pendingChange;
            private readonly TfsApiBridge _bridge;
            private readonly IVersionControlServer _versionControlServer;
            private long _contentLength = -1;

            public FakeItem(PendingChange pendingChange, TfsApiBridge bridge, IVersionControlServer versionControlServer)
            {
                _pendingChange = pendingChange;
                _bridge = bridge;
                _versionControlServer = versionControlServer;
            }

            public IVersionControlServer VersionControlServer
            {
                get { return _versionControlServer; }
            }

            public int ChangesetId
            {
                get
                {
                    // some operations like applying rename gets previous item state
                    // via looking at version of item minus 1. So will try to emulate
                    // that this shelve is real revision.
                    return _pendingChange.Version + 1;
                }
            }

            public string ServerItem
            {
                get { return _pendingChange.ServerItem; }
            }

            public decimal DeletionId
            {
                get { return _pendingChange.DeletionId; }
            }

            public TfsItemType ItemType
            {
                get { return _bridge.Convert<TfsItemType>(_pendingChange.ItemType); }
            }

            public int ItemId
            {
                get { return _pendingChange.ItemId; }
            }

            public long ContentLength
            {
                get
                {
                    if (_contentLength < 0)
                        throw new InvalidOperationException("You can't query ContentLength before downloading the file");
                    // It is not great solution, but at least makes the contract explicit.
                    // We can't actually save downloaded file in this class, because if nobody asks
                    // for it - we won't know when it is safe to delete it and it will stay in the 
                    // system forever, which is bad. Implementing finalizer to delete file is also bad solution:
                    // suppose process was killed in the middle of many-megabyte operation on thousands of files
                    // if we delete them as soon as they are not used - only current file will remain. Otherwise
                    // all of them.
                    // With this exception at least it would be evident asap that something went wrong, so we could fix it.
                    return _contentLength;
                }
            }

            public TemporaryFile DownloadFile()
            {
                var temp = new TemporaryFile();
                _pendingChange.DownloadShelvedFile(temp);
                _contentLength = new FileInfo(temp).Length;
                return temp;
            }
        }

        #endregion

        public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
        {
            var shelveset = new Shelveset(_bridge.Unwrap<Workspace>(workspace).VersionControlServer, shelvesetName, workspace.OwnerName);
            return _bridge.Wrap<WrapperForShelveset, Shelveset>(shelveset);
        }

        public IIdentity GetIdentity(string username)
        {
            return _bridge.Wrap<WrapperForIdentity, Identity>(GroupSecurityService.ReadIdentity(SearchFactor.AccountName, username, QueryMembership.None));
        }

        public ITfsChangeset GetLatestChangeset(GitTfsRemote remote)
        {
            var history = VersionControl.QueryHistory(remote.TfsRepositoryPath, VersionSpec.Latest, 0,
                                                      RecursionType.Full, null, null, VersionSpec.Latest, 1, true, false,
                                                      false).Cast<Changeset>().ToList();

            if (history.Empty())
                throw new GitTfsException("error: remote TFS repository path was not found");

            return BuildTfsChangeset(history.Single(), remote);
        }

        public IChangeset GetChangeset(int changesetId)
        {
            return _bridge.Wrap<WrapperForChangeset, Changeset>(VersionControl.GetChangeset(changesetId));
        }

        public ITfsChangeset GetChangeset(int changesetId, GitTfsRemote remote)
        {
            return BuildTfsChangeset(VersionControl.GetChangeset(changesetId), remote);
        }

        public bool MatchesUrl(string tfsUrl)
        {
            return Url == tfsUrl || LegacyUrls.Contains(tfsUrl);
        }

        public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            return
                GetWorkItemInfosHelper<IWorkItemCheckinInfo, WrapperForWorkItemCheckinInfo, WorkItemCheckinInfo>(
                    workItems, checkinAction, GetWorkItemInfo);
        }

        public IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            return
                GetWorkItemInfosHelper<IWorkItemCheckedInfo, WrapperForWorkItemCheckedInfo, WorkItemCheckedInfo>(
                    workItems, checkinAction, GetWorkItemCheckedInfo);
        }

        private IEnumerable<TInterface> GetWorkItemInfosHelper<TInterface, TWrapper, TInstance>(
            IEnumerable<string> workItems,
            TfsWorkItemCheckinAction checkinAction,
            Func<string, WorkItemCheckinAction, TInstance> func
            )
            where TWrapper : class
        {
            return (from workItem in workItems
                    select _bridge.Wrap<TWrapper, TInstance>(
                        func(workItem, _bridge.Convert<WorkItemCheckinAction>(checkinAction))))
                .Cast<TInterface>();
        }

        private WorkItemCheckinInfo GetWorkItemInfo(string workItem, WorkItemCheckinAction checkinAction)
        {
            return new WorkItemCheckinInfo(WorkItems.GetWorkItem(Convert.ToInt32(workItem)), checkinAction);
        }

        private static WorkItemCheckedInfo GetWorkItemCheckedInfo(string workitem, WorkItemCheckinAction checkinAction)
        {
            return new WorkItemCheckedInfo(Convert.ToInt32(workitem), true, checkinAction);
        }
    }
}