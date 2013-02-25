using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;

namespace Sep.Git.Tfs.VsFake
{
    public class TfsHelper : ITfsHelper, IVersionControlServer
    {
        #region misc/null

        IContainer _container;
        TextWriter _stdout;
        Script _script;
        Dictionary<int, IdMap> _itemIds;

        public TfsHelper(IContainer container, TextWriter stdout, Script script)
        {
            _container = container;
            _stdout = stdout;
            _script = script;
            if(_script != null)
                InitializeItemIds();
        }

        public string TfsClientLibraryVersion { get { return "(FAKE)"; } }

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public void EnsureAuthenticated() {}

        public bool CanShowCheckinDialog { get { return false; } }

        public long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string username)
        {
            return new NullIdentity();
        }

        #endregion

        #region read changesets

        public ITfsChangeset GetLatestChangeset(GitTfsRemote remote)
        {
            return _script.Changesets.LastOrDefault().AndAnd(x => BuildTfsChangeset(x, remote));
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote)
        {
            return _script.Changesets.Where(x => x.Id >= startVersion).Select(x => BuildTfsChangeset(x, remote));
        }

        private ITfsChangeset BuildTfsChangeset(ScriptedChangeset changeset, GitTfsRemote remote)
        {
            var tfsChangeset = _container.With<ITfsHelper>(this).With<IChangeset>(new Changeset(changeset, this, _itemIds[changeset.Id])).GetInstance<TfsChangeset>();
            tfsChangeset.Summary = new TfsChangesetInfo { ChangesetId = changeset.Id, Remote = remote };
            return tfsChangeset;
        }

        class Changeset : IChangeset
        {
            private ScriptedChangeset _changeset;
            private TfsHelper _server;
            private IdMap _itemIds;

            public Changeset(ScriptedChangeset changeset, TfsHelper server, IdMap itemIds)
            {
                _changeset = changeset;
                _server = server;
                _itemIds = itemIds;
            }

            public IChange[] Changes
            {
                get { return _changeset.Changes.Select(x => new Change(_changeset, x, _server, _itemIds[x])).ToArray(); }
            }

            public string Committer
            {
                get { return "todo"; }
            }

            public DateTime CreationDate
            {
                get { return _changeset.CheckinDate; }
            }

            public string Comment
            {
                get { return _changeset.Comment; }
            }

            public int ChangesetId
            {
                get { return _changeset.Id; }
            }

            public IVersionControlServer VersionControlServer
            {
                get { return _server; }
            }

            public void Get(IWorkspace workspace)
            {
                workspace.GetSpecificVersion(this);
            }
        }

        class Change : IChange, IItem
        {
            ScriptedChangeset _changeset;
            ScriptedChange _change;
            TfsHelper _server;
            int _itemId;

            public Change(ScriptedChangeset changeset, ScriptedChange change, TfsHelper server, int itemId)
            {
                _changeset = changeset;
                _change = change;
                _server = server;
                _itemId = itemId;
            }

            TfsChangeType IChange.ChangeType
            {
                get { return _change.ChangeType; }
            }

            IItem IChange.Item
            {
                get { return this; }
            }

            IVersionControlServer IItem.VersionControlServer
            {
                get { return _server; }
            }

            int IItem.ChangesetId
            {
                get { return _changeset.Id; }
            }

            string IItem.ServerItem
            {
                get { return _change.RepositoryPath; }
            }

            int IItem.DeletionId
            {
                get { return 0; }
            }

            TfsItemType IItem.ItemType
            {
                get { return _change.ItemType; }
            }

            int IItem.ItemId
            {
                get { return _itemId; }
            }

            long IItem.ContentLength
            {
                get
                {
                    using (var temp = ((IItem)this).DownloadFile())
                        return new FileInfo(temp).Length;
                }
            }

            TemporaryFile IItem.DownloadFile()
            {
                var temp = new TemporaryFile();
                using (var writer = new StreamWriter(temp))
                    writer.Write(Content);
                return temp;
            }

            public string Content
            {
                get
                {
                    return _server.GetContent(_changeset, _change);
                }
            }
        }

        #endregion

        #region workspaces

        public void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            Trace.WriteLine("Setting up a TFS workspace at " + directory);
            var fakeWorkspace = new FakeWorkspace(directory, remote.TfsRepositoryPath);
            var workspace = _container.With("localDirectory").EqualTo(directory)
                .With("remote").EqualTo(remote)
                .With("contextVersion").EqualTo(versionToFetch)
                .With("workspace").EqualTo(fakeWorkspace)
                .With("tfsHelper").EqualTo(this)
                .GetInstance<TfsWorkspace>();
            action(workspace);
        }

        class FakeWorkspace : IWorkspace
        {
            string _directory;
            string _repositoryRoot;

            public FakeWorkspace(string directory, string repositoryRoot)
            {
                _directory = directory;
                _repositoryRoot = repositoryRoot;
            }

            public void GetSpecificVersion(IChangeset changeset)
            {
                var repositoryRoot = _repositoryRoot.ToLower();
                if(!repositoryRoot.EndsWith("/")) repositoryRoot += "/";
                foreach (var change in changeset.Changes)
                {
                    if (change.Item.ItemType == TfsItemType.File)
                    {
                        var outPath = Path.Combine(_directory, change.Item.ServerItem.ToLower().Replace(repositoryRoot, ""));
                        var outDir = Path.GetDirectoryName(outPath);
                        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                        using (var download = change.Item.DownloadFile())
                            File.WriteAllText(outPath, File.ReadAllText(download.Path));
                    }
                }
            }

            #region unimplemented

            public IPendingChange[] GetPendingChanges()
            {
                throw new NotImplementedException();
            }

            public ICheckinEvaluationResult EvaluateCheckin(TfsCheckinEvaluationOptions options, IPendingChange[] allChanges, IPendingChange[] changes, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges)
            {
                throw new NotImplementedException();
            }

            public void Shelve(IShelveset shelveset, IPendingChange[] changes, TfsShelvingOptions options)
            {
                throw new NotImplementedException();
            }

            public int Checkin(IPendingChange[] changes, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges, TfsPolicyOverrideInfo policyOverrideInfo, bool overrideGatedCheckIn)
            {
                throw new NotImplementedException();
            }

            public int PendAdd(string path)
            {
                throw new NotImplementedException();
            }

            public int PendEdit(string path)
            {
                throw new NotImplementedException();
            }

            public int PendDelete(string path)
            {
                throw new NotImplementedException();
            }

            public int PendRename(string pathFrom, string pathTo)
            {
                throw new NotImplementedException();
            }

            public void ForceGetFile(string path, int changeset)
            {
                throw new NotImplementedException();
            }

            public void GetSpecificVersion(int changeset)
            {
                throw new NotImplementedException();
            }

            public string GetLocalItemForServerItem(string serverItem)
            {
                throw new NotImplementedException();
            }

            public string OwnerName
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        #endregion

        #region item ids, history, continuity, etc.

        IItem IVersionControlServer.GetItem(int itemId, int changesetNumber)
        {
            return new HistoricalItem(_itemIds[changesetNumber][itemId]);
        }

        public string GetContent(ScriptedChangeset changeset, ScriptedChange change)
        {
            if(change.Content != null)
            {
                return change.Content;
            }
            if(change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
            {
                var itemId = _itemIds[changeset.Id][change];
                var previousChangesets = _script.Changesets.Where(cs => cs.Id < changeset.Id).OrderByDescending(cs => cs.Id);
                foreach(var previousChangeset in previousChangesets)
                {
                    var previousChange = previousChangeset.Changes.Where(c => itemId == _itemIds[previousChangeset.Id][c]).FirstOrDefault();
                    if(previousChange != null)
                    {
                        return GetContent(previousChangeset, previousChange);
                    }
                }
            }
            return null;
        }

        class HistoricalItem : IItem
        {
            string _path;

            public HistoricalItem(string path)
            {
                _path = path;
            }

            public string ServerItem
            {
                get { return _path; }
            }

            public IVersionControlServer VersionControlServer
            {
                get { throw new NotImplementedException(); }
            }

            public int ChangesetId
            {
                get { throw new NotImplementedException(); }
            }

            public int DeletionId
            {
                get { throw new NotImplementedException(); }
            }

            public TfsItemType ItemType
            {
                get { throw new NotImplementedException(); }
            }

            public int ItemId
            {
                get { throw new NotImplementedException(); }
            }

            public long ContentLength
            {
                get { throw new NotImplementedException(); }
            }

            public TemporaryFile DownloadFile()
            {
                throw new NotImplementedException();
            }
        }

        private void InitializeItemIds()
        {
            _itemIds = new Dictionary<int, IdMap>();
            var current = new IdMap();
            foreach (var changeset in _script.Changesets)
            {
                foreach (var change in changeset.Changes)
                {
                    current.Update(change);
                }
                _itemIds.Add(changeset.Id, current);
                current = new IdMap(current);
            }
        }

        class IdMap
        {
            static int lastItemId = 0;

            IdMap previousMap;
            Dictionary<int, string> idToPath = new Dictionary<int, string>();
            Dictionary<string, int> pathToId = new Dictionary<string, int>();

            public int this[ScriptedChange change] { get { return this[change.RepositoryPath]; } }
            public int this[string path] { get { return pathToId.ContainsKey(path) ? pathToId[path] : previousMap[path]; } }
            public string this[int id] { get { return idToPath.ContainsKey(id) ? idToPath[id] : previousMap[id]; } }

            public IdMap(IdMap previousMap = null)
            {
                this.previousMap = previousMap;
            }

            /// <summary>
            /// Update this IdMap with the provided change.
            /// </summary>
            /// <param name="change"></param>
            public void Update(ScriptedChange change)
            {
                if (change.ChangeType.IncludesOneOf(TfsChangeType.Add))
                {
                    var id = ++lastItemId;
                    pathToId.Add(change.RepositoryPath, id);
                    idToPath.Add(id, change.RepositoryPath);
                }
                else if (change.ChangeType.IncludesOneOf(TfsChangeType.Rename))
                {
                    var renamedItemId = previousMap[change.RenamedFrom];
                    pathToId.Add(change.RepositoryPath, renamedItemId);
                    idToPath.Add(renamedItemId, change.RepositoryPath);
                }
                else if (change.ChangeType.IncludesOneOf(TfsChangeType.Delete))
                {
                    // Do nothing.
                    // This isn't strictly "correct", but it is good enough for the tests.
                }
            }

            public string Inspect()
            {
                var s = idToPath.Select(x => String.Format("{0} - {1}\n", x.Key, x.Value)).Aggregate("", (s1, s2) => s1 + s2);
                return (previousMap == null ? "" : previousMap.Inspect() + "^^^^\n") + s;
            }
        }

        #endregion

        #region unimplemented

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

        public ICheckinNote CreateCheckinNote(Dictionary<string, string> checkinNotes)
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

        public void CleanupWorkspaces(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        public bool CanGetBranchInformation { get { return false; } }

        public int GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IBranchObject> GetBranches()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TfsLabel> GetLabels(string tfsPathBranch, string nameFilter = null)
        {
            throw new NotImplementedException();
        }

        public void CreateBranch(string sourcePath, string targetPath, int changesetId, string comment = null)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IVersionControlServer

        IItem IVersionControlServer.GetItem(string itemPath, int changesetNumber)
        {
            throw new NotImplementedException();
        }

        IItem[] IVersionControlServer.GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType)
        {
            throw new NotImplementedException();
        }

        IEnumerable<IChangeset> IVersionControlServer.QueryHistory(string path, int version, int deletionId, TfsRecursionType recursion, string user, int versionFrom, int versionTo, int maxCount, bool includeChanges, bool slotMode, bool includeDownloadInfo)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
