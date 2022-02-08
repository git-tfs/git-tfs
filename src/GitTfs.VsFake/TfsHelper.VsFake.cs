using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using GitTfs.Util;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitTfs.VsFake
{
    public class MockBranchObject : IBranchObject
    {
        public string Path { get; set; }

        public string ParentPath { get; set; }

        public bool IsRoot { get; set; }
    }

    public class TfsHelper : ITfsHelper
    {
        #region misc/null

        private readonly IContainer _container;
        private readonly Script _script;
        private readonly FakeVersionControlServer _versionControlServer;

        public TfsHelper(IContainer container, Script script)
        {
            _container = container;
            _script = script;
            _versionControlServer = new FakeVersionControlServer(_script);
        }

        public string TfsClientLibraryVersion { get { return "(FAKE)"; } }

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public void EnsureAuthenticated() { }

        public void SetPathResolver() { }

        public bool CanShowCheckinDialog { get { return false; } }

        public int ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string username)
        {
            if (username == "vtccds_cp")
                return new FakeIdentity { DisplayName = username, MailAddress = "b8d46dada4dd62d2ab98a2bda7310285c42e46f6qvabajY2" };
            return new NullIdentity();
        }

        #endregion

        #region read changesets

        public ITfsChangeset GetLatestChangeset(IGitTfsRemote remote)
        {
            return _script.Changesets.LastOrDefault().Try(x => BuildTfsChangeset(x, remote));
        }

        public int GetLatestChangesetId(IGitTfsRemote remote)
        {
            return _script.Changesets.LastOrDefault().Id;
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, int startVersion, IGitTfsRemote remote, int lastVersion = -1, bool byLots = false)
        {
            if (!_script.Changesets.Any(c => c.IsBranchChangeset) && _script.Changesets.Any(c => c.IsMergeChangeset))
                return _script.Changesets.Where(x => x.Id >= startVersion).Select(x => BuildTfsChangeset(x, remote));
            var branchPath = path + "/";
            return _script.Changesets
                .Where(x => x.Id >= startVersion && x.Changes.Any(c => c.RepositoryPath.IndexOf(branchPath, StringComparison.CurrentCultureIgnoreCase) == 0 || branchPath.IndexOf(c.RepositoryPath, StringComparison.CurrentCultureIgnoreCase) == 0))
                .Select(x => BuildTfsChangeset(x, remote));
        }

        public int FindMergeChangesetParent(string path, int firstChangeset, GitTfsRemote remote)
        {
            var firstChangesetOfBranch = _script.Changesets.FirstOrDefault(c => c.IsMergeChangeset && c.MergeChangesetDatas.MergeIntoBranch == path && c.MergeChangesetDatas.BeforeMergeChangesetId < firstChangeset);
            if (firstChangesetOfBranch != null)
                return firstChangesetOfBranch.MergeChangesetDatas.BeforeMergeChangesetId;
            return -1;
        }

        private ITfsChangeset BuildTfsChangeset(ScriptedChangeset changeset, IGitTfsRemote remote)
        {
            TfsChangesetInfo tfsChangesetInfo = new TfsChangesetInfo { ChangesetId = changeset.Id, Remote = remote };
            var tfsChangeset = _container.With<ITfsHelper>(this).With<IChangeset>(new Changeset(_versionControlServer, changeset)).With(tfsChangesetInfo).GetInstance<TfsChangeset>();
            return tfsChangeset;
        }

        private class Changeset : IChangeset
        {
            private readonly IVersionControlServer _versionControlServer;
            private readonly ScriptedChangeset _changeset;

            public Changeset(IVersionControlServer versionControlServer, ScriptedChangeset changeset)
            {
                _versionControlServer = versionControlServer;
                _changeset = changeset;
            }

            public IChange[] Changes
            {
                get { return _changeset.Changes.Select(x => new Change(_versionControlServer, _changeset, x)).ToArray(); }
            }

            public string Committer
            {
                get { return _changeset.Committer ?? "todo"; }
            }

            public DateTime CreationDate
            {
                get { return _changeset.CheckinDate; }
            }

            public string Comment
            {
                get { return _changeset.Comment.Replace("\n", "\r\n"); }
            }

            public int ChangesetId
            {
                get { return _changeset.Id; }
            }

            public IVersionControlServer VersionControlServer
            {
                get { throw new NotImplementedException(); }
            }

            public void Get(ITfsWorkspace workspace, IEnumerable<IChange> changes, Action<Exception> ignorableErrorHandler)
            {
                workspace.Get(ChangesetId, changes);
            }
        }

        private class Change : IChange, IItem
        {
            private readonly IVersionControlServer _versionControlServer;
            private readonly ScriptedChangeset _changeset;
            private readonly ScriptedChange _change;

            public Change(IVersionControlServer versionControlServer, ScriptedChangeset changeset, ScriptedChange change)
            {
                _versionControlServer = versionControlServer;
                _changeset = changeset;
                _change = change;
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
                get { return _versionControlServer; }
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
                get { return _change.ItemId.Value; }
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
                using (var stream = File.Create(temp))
                using (var writer = new BinaryWriter(stream))
                    writer.Write(_change.Content);
                return temp;
            }
        }

        #endregion

        #region workspaces

        public void WithWorkspace(string localDirectory, IGitTfsRemote remote, IEnumerable<Tuple<string, string>> mappings, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            Trace.WriteLine("Setting up a TFS workspace at " + localDirectory);
            var fakeWorkspace = new FakeWorkspace(localDirectory, remote.TfsRepositoryPath);
            var workspace = _container.With("localDirectory").EqualTo(localDirectory)
                .With("remote").EqualTo(remote)
                .With("contextVersion").EqualTo(versionToFetch)
                .With("workspace").EqualTo(fakeWorkspace)
                .With("tfsHelper").EqualTo(this)
                .GetInstance<TfsWorkspace>();
            action(workspace);
        }

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

        private class FakeWorkspace : IWorkspace
        {
            private readonly string _directory;
            private readonly string _repositoryRoot;

            public FakeWorkspace(string directory, string repositoryRoot)
            {
                _directory = directory;
                _repositoryRoot = repositoryRoot;
            }

            public void GetSpecificVersion(int changesetId, IEnumerable<IItem> items, bool noParallel)
            {
                throw new NotImplementedException();
            }

            public void GetSpecificVersion(IChangeset changeset, bool noParallel)
            {
                GetSpecificVersion(changeset.ChangesetId, changeset.Changes, noParallel);
            }

            public void GetSpecificVersion(int changeset, IEnumerable<IChange> changes, bool noParallel)
            {
                var repositoryRoot = _repositoryRoot.ToLower();
                if (!repositoryRoot.EndsWith("/")) repositoryRoot += "/";
                foreach (var change in changes)
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

            public void Merge(string sourceTfsPath, string tfsRepositoryPath)
            {
                throw new NotImplementedException();
            }

            public IPendingChange[] GetPendingChanges()
            {
                throw new NotImplementedException();
            }

            public ICheckinEvaluationResult EvaluateCheckin(TfsCheckinEvaluationOptions options, IPendingChange[] allChanges, IPendingChange[] changes, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges)
            {
                throw new NotImplementedException();
            }

            public ICheckinEvaluationResult EvaluateCheckin(TfsCheckinEvaluationOptions options, IPendingChange[] allChanges, IPendingChange[] changes, string comment, string authors, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges)
            {
                throw new NotImplementedException();
            }

            public void Shelve(IShelveset shelveset, IPendingChange[] changes, TfsShelvingOptions options)
            {
                throw new NotImplementedException();
            }

            public int Checkin(IPendingChange[] changes, string comment, string author, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges, TfsPolicyOverrideInfo policyOverrideInfo, bool overrideGatedCheckIn)
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

            public string GetServerItemForLocalItem(string localItem)
            {
                throw new NotImplementedException();
            }

            public string OwnerName
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        public void CleanupWorkspaces(string workingDirectory)
        {
        }

        public bool IsExistingInTfs(string path)
        {
            var exists = false;
            foreach (var changeset in _script.Changesets)
            {
                foreach (var change in changeset.Changes)
                {
                    if (change.RepositoryPath == path)
                    {
                        exists = !change.ChangeType.IncludesOneOf(TfsChangeType.Delete);
                    }
                }
            }
            return exists;
        }

        public IChangeset GetChangeset(int changesetId)
        {
            return new Changeset(_versionControlServer, _script.Changesets.First(c => c.Id == changesetId));
        }

        public IList<RootBranch> GetRootChangesetForBranch(string tfsPathBranchToCreate, int lastChangesetIdToCheck = -1, string tfsPathParentBranch = null)
        {
            var branchChangesets = _script.Changesets.Where(c => c.IsBranchChangeset);
            var firstBranchChangeset = branchChangesets.FirstOrDefault(c => c.BranchChangesetDatas.BranchPath == tfsPathBranchToCreate);

            var rootBranches = new List<RootBranch>();
            if (firstBranchChangeset != null)
            {
                do
                {
                    var rootBranch = new RootBranch(
                        firstBranchChangeset.BranchChangesetDatas.RootChangesetId,
                        firstBranchChangeset.Id,
                        firstBranchChangeset.BranchChangesetDatas.BranchPath
                    );

                    rootBranch.IsRenamedBranch = DeletedBranchesPathes.Contains(rootBranch.TfsBranchPath);
                    rootBranches.Add(rootBranch);

                    firstBranchChangeset = branchChangesets.FirstOrDefault(c => c.BranchChangesetDatas.BranchPath == firstBranchChangeset.BranchChangesetDatas.ParentBranch);
                } while (firstBranchChangeset != null);
                rootBranches.Reverse();
                return rootBranches;
            }
            rootBranches.Add(new RootBranch(-1, tfsPathBranchToCreate));
            return rootBranches;
        }

        private List<string> _deletedBranchesPathes;
        private List<string> DeletedBranchesPathes
        {
            get
            {
                return _deletedBranchesPathes ?? (_deletedBranchesPathes = _script.Changesets.Where(c => c.IsBranchChangeset &&
                      c.Changes.Any(ch => ch.ChangeType == TfsChangeType.Delete && ch.RepositoryPath == c.BranchChangesetDatas.ParentBranch))
                      .Select(b => b.BranchChangesetDatas.ParentBranch).ToList());
            }
        }

        public IEnumerable<IBranchObject> GetBranches(bool getDeletedBranches = false)
        {
            var renamings = _script.Changesets.Where(
                c => c.IsBranchChangeset &&
                DeletedBranchesPathes.Any(b => b == c.BranchChangesetDatas.BranchPath)).ToList();

            var branches = new List<IBranchObject>();
            branches.AddRange(_script.RootBranches.Select(b => new MockBranchObject { IsRoot = true, Path = b.BranchPath, ParentPath = null }));
            branches.AddRange(_script.Changesets.Where(c => c.IsBranchChangeset).Select(c => new MockBranchObject
            {
                IsRoot = false,
                Path = c.BranchChangesetDatas.BranchPath,
                ParentPath = GetRealRootBranch(renamings, c.BranchChangesetDatas.ParentBranch)
            }));
            if (!getDeletedBranches)
                branches.RemoveAll(b => DeletedBranchesPathes.Contains(b.Path));

            return branches;
        }

        private string GetRealRootBranch(List<ScriptedChangeset> deletedBranches, string branchPath)
        {
            var realRoot = branchPath;
            while (true)
            {
                var parent = deletedBranches.FirstOrDefault(b => b.BranchChangesetDatas.BranchPath == realRoot);
                if (parent == null)
                    return realRoot;
                realRoot = parent.BranchChangesetDatas.ParentBranch;
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

        public ITfsChangeset GetChangeset(int changesetId, IGitTfsRemote remote)
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

        public IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            return new List<string>();
        }

        public IEnumerable<TfsLabel> GetLabels(string tfsPathBranch, string nameFilter = null)
        {
            throw new NotImplementedException();
        }

        public void CreateBranch(string sourcePath, string targetPath, int changesetId, string comment = null)
        {
            throw new NotImplementedException();
        }

        public void CreateTfsRootBranch(string projectName, string mainBranch, string gitRepositoryPath, bool createTeamProjectFolder)
        {
            throw new NotImplementedException();
        }

        public int QueueGatedCheckinBuild(Uri value, string buildDefinitionName, string shelvesetName, string checkInTicket)
        {
            throw new NotImplementedException();
        }

        public void DeleteShelveset(IWorkspace workspace, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        #endregion

        private class FakeVersionControlServer : IVersionControlServer
        {
            private readonly Script _script;

            public FakeVersionControlServer(Script script)
            {
                _script = script;
            }

            public IItem GetItem(int itemId, int changesetNumber)
            {
                var match = _script.Changesets.AsEnumerable().Reverse()
                    .SkipWhile(cs => cs.Id > changesetNumber)
                    .Select(cs => new { Changeset = cs, Change = cs.Changes.SingleOrDefault(change => change.ItemId == itemId) })
                    .First(x => x.Change != null);
                return new Change(this, match.Changeset, match.Change);
            }

            public IItem GetItem(string itemPath, int changesetNumber)
            {
                throw new NotImplementedException();
            }

            public IItem[] GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IChangeset> QueryHistory(string path, int version, int deletionId, TfsRecursionType recursion, string user, int versionFrom, int versionTo, int maxCount, bool includeChanges, bool slotMode, bool includeDownloadInfo)
            {
                throw new NotImplementedException();
            }
        }
    }
}
