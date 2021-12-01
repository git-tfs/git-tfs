using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using GitTfs.Util;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitTfs.VsCommon
{
    public class WrapperForVersionControlServer : WrapperFor<VersionControlServer>, IVersionControlServer
    {
        private readonly TfsApiBridge _bridge;
        private readonly VersionControlServer _versionControlServer;

        public WrapperForVersionControlServer(TfsApiBridge bridge, VersionControlServer versionControlServer) : base(versionControlServer)
        {
            _bridge = bridge;
            _versionControlServer = versionControlServer;
        }

        public IItem GetItem(int itemId, int changesetNumber)
        {
            return _bridge.Wrap<WrapperForItem, Item>(_versionControlServer.GetItem(itemId, changesetNumber));
        }

        public IItem GetItem(string itemPath, int changesetNumber)
        {
            return _bridge.Wrap<WrapperForItem, Item>(_versionControlServer.GetItem(itemPath, new ChangesetVersionSpec(changesetNumber)));
        }

        public IItem[] GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType)
        {
            var itemSet = _versionControlServer.GetItems(
                new ItemSpec(itemPath, _bridge.Convert<RecursionType>(recursionType), 0),
                new ChangesetVersionSpec(changesetNumber),
                DeletedState.NonDeleted,
                ItemType.Any,
                // do not load the loading info
                false);

            return _bridge.Wrap<WrapperForItem, Item>(itemSet.Items);
        }

        public IEnumerable<IChangeset> QueryHistory(string path, int version, int deletionId,
                                                    TfsRecursionType recursion, string user, int versionFrom, int versionTo, int maxCount,
                                                    bool includeChanges, bool slotMode, bool includeDownloadInfo)
        {
            var history = _versionControlServer.QueryHistory(path, new ChangesetVersionSpec(version), deletionId,
                                                             _bridge.Convert<RecursionType>(recursion), user, new ChangesetVersionSpec(versionFrom),
                                                             new ChangesetVersionSpec(versionTo), maxCount, includeChanges, slotMode,
                                                             includeDownloadInfo);
            return _bridge.Wrap<WrapperForChangeset, Changeset>(history);
        }
    }

    public class WrapperForChangeset : WrapperFor<Changeset>, IChangeset
    {
        private readonly TfsApiBridge _bridge;
        private readonly Changeset _changeset;

        public WrapperForChangeset(TfsApiBridge bridge, Changeset changeset) : base(changeset)
        {
            _bridge = bridge;
            _changeset = changeset;
        }

        public IChange[] Changes
        {
            get { return _bridge.Wrap<WrapperForChange, Change>(_changeset.Changes); }
        }

        public string Committer
        {
            get
            {
                var committer = _changeset.Committer;
                var owner = _changeset.Owner;

                // Sometimes TFS itself commits the changeset
                if (owner != committer)
                    return owner;

                return committer;
            }
        }

        public DateTime CreationDate
        {
            get { return _changeset.CreationDate; }
        }

        public string Comment
        {
            get { return _changeset.Comment; }
        }

        public int ChangesetId
        {
            get { return _changeset.ChangesetId; }
        }

        public IVersionControlServer VersionControlServer
        {
            get { return _bridge.Wrap<WrapperForVersionControlServer, VersionControlServer>(_changeset.VersionControlServer); }
        }

        public void Get(ITfsWorkspace workspace, IEnumerable<IChange> changes, Action<Exception> ignorableErrorHandler)
        {
            workspace.Get(ChangesetId, changes);
        }
    }

    public class WrapperForChange : WrapperFor<Change>, IChange
    {
        private readonly TfsApiBridge _bridge;
        private readonly Change _change;

        public WrapperForChange(TfsApiBridge bridge, Change change) : base(change)
        {
            _bridge = bridge;
            _change = change;
        }

        public TfsChangeType ChangeType
        {
            get { return _bridge.Convert<TfsChangeType>(_change.ChangeType); }
        }

        public IItem Item
        {
            get { return _bridge.Wrap<WrapperForItem, Item>(_change.Item); }
        }
    }

    public class WrapperForItem : WrapperFor<Item>, IItem
    {
        private readonly TfsApiBridge _bridge;
        private readonly Item _item;

        public WrapperForItem(TfsApiBridge bridge, Item item) : base(item)
        {
            _bridge = bridge;
            _item = item;
        }

        public IVersionControlServer VersionControlServer
        {
            get { return _bridge.Wrap<WrapperForVersionControlServer, VersionControlServer>(_item.VersionControlServer); }
        }

        public int ChangesetId
        {
            get { return _item.ChangesetId; }
        }

        public string ServerItem
        {
            get { return _item.ServerItem; }
        }

        public int DeletionId
        {
            get { return _item.DeletionId; }
        }

        public TfsItemType ItemType
        {
            get { return _bridge.Convert<TfsItemType>(_item.ItemType); }
        }

        public int ItemId
        {
            get { return _item.ItemId; }
        }

        public long ContentLength
        {
            get { return _item.ContentLength; }
        }

        public TemporaryFile DownloadFile()
        {
            var temp = new TemporaryFile();
            try
            {
                _bridge.Unwrap<Item>(_item).DownloadFile(temp);
                return temp;
            }
            catch (Exception)
            {
                Trace.WriteLine(string.Format("Something went wrong downloading \"{0}\" in changeset {1}", _item.ServerItem, _item.ChangesetId));
                temp.Dispose();
                throw;
            }
        }
    }

    public class WrapperForIdentity : WrapperFor<Identity>, IIdentity
    {
        private readonly Identity _identity;

        public WrapperForIdentity(Identity identity) : base(identity)
        {
            Debug.Assert(identity != null, "wrapped property must not be null.");
            _identity = identity;
        }

        public string MailAddress
        {
            get { return _identity.MailAddress; }
        }

        public string DisplayName
        {
            get { return _identity.DisplayName; }
        }
    }

    public class WrapperForShelveset : WrapperFor<Shelveset>, IShelveset
    {
        private readonly Shelveset _shelveset;
        private readonly TfsApiBridge _bridge;

        public WrapperForShelveset(TfsApiBridge bridge, Shelveset shelveset) : base(shelveset)
        {
            _shelveset = shelveset;
            _bridge = bridge;
        }

        public string Comment
        {
            get { return _shelveset.Comment; }
            set { _shelveset.Comment = value; }
        }

        public IWorkItemCheckinInfo[] WorkItemInfo
        {
            get { return _bridge.Wrap<WrapperForWorkItemCheckinInfo, WorkItemCheckinInfo>(_shelveset.WorkItemInfo); }
            set { _shelveset.WorkItemInfo = _bridge.Unwrap<WorkItemCheckinInfo>(value); }
        }
    }

    public class WrapperForWorkItemCheckinInfo : WrapperFor<WorkItemCheckinInfo>, IWorkItemCheckinInfo
    {
        public WrapperForWorkItemCheckinInfo(WorkItemCheckinInfo workItemCheckinInfo) : base(workItemCheckinInfo)
        {
        }
    }

    public class WrapperForWorkItemCheckedInfo : WrapperFor<WorkItemCheckedInfo>, IWorkItemCheckedInfo
    {
        public WrapperForWorkItemCheckedInfo(WorkItemCheckedInfo workItemCheckinInfo)
            : base(workItemCheckinInfo)
        {
        }
    }

    public class WrapperForPendingChange : WrapperFor<PendingChange>, IPendingChange
    {
        public WrapperForPendingChange(PendingChange pendingChange) : base(pendingChange)
        {
        }
    }

    public class WrapperForCheckinNote : WrapperFor<CheckinNote>, ICheckinNote
    {
        public WrapperForCheckinNote(CheckinNote checkiNote) : base(checkiNote)
        {
        }
    }

    public class WrapperForCheckinEvaluationResult : WrapperFor<CheckinEvaluationResult>, ICheckinEvaluationResult
    {
        private readonly TfsApiBridge _bridge;
        private readonly CheckinEvaluationResult _result;

        public WrapperForCheckinEvaluationResult(TfsApiBridge bridge, CheckinEvaluationResult result) : base(result)
        {
            _bridge = bridge;
            _result = result;
        }

        public ICheckinConflict[] Conflicts
        {
            get { return _bridge.Wrap<WrapperForCheckinConflict, CheckinConflict>(_result.Conflicts); }
        }

        public ICheckinNoteFailure[] NoteFailures
        {
            get { return _bridge.Wrap<WrapperForCheckinNoteFailure, CheckinNoteFailure>(_result.NoteFailures); }
        }

        public IPolicyFailure[] PolicyFailures
        {
            get { return _bridge.Wrap<WrapperForPolicyFailure, PolicyFailure>(_result.PolicyFailures); }
        }

        public Exception PolicyEvaluationException
        {
            get { return _result.PolicyEvaluationException; }
        }
    }

    public class WrapperForCheckinConflict : WrapperFor<CheckinConflict>, ICheckinConflict
    {
        private readonly CheckinConflict _conflict;

        public WrapperForCheckinConflict(CheckinConflict conflict) : base(conflict)
        {
            _conflict = conflict;
        }

        public string ServerItem
        {
            get { return _conflict.ServerItem; }
        }

        public string Message
        {
            get { return _conflict.Message; }
        }

        public bool Resolvable
        {
            get { return _conflict.Resolvable; }
        }
    }

    public class WrapperForCheckinNoteFailure : WrapperFor<CheckinNoteFailure>, ICheckinNoteFailure
    {
        private readonly TfsApiBridge _bridge;
        private readonly CheckinNoteFailure _failure;

        public WrapperForCheckinNoteFailure(TfsApiBridge bridge, CheckinNoteFailure failure) : base(failure)
        {
            _bridge = bridge;
            _failure = failure;
        }

        public ICheckinNoteFieldDefinition Definition
        {
            get { return _bridge.Wrap<WrapperForCheckinNoteFieldDefinition, CheckinNoteFieldDefinition>(_failure.Definition); }
        }

        public string Message
        {
            get { return _failure.Message; }
        }
    }

    public class WrapperForCheckinNoteFieldDefinition : WrapperFor<CheckinNoteFieldDefinition>, ICheckinNoteFieldDefinition
    {
        private readonly CheckinNoteFieldDefinition _fieldDefinition;

        public WrapperForCheckinNoteFieldDefinition(CheckinNoteFieldDefinition fieldDefinition) : base(fieldDefinition)
        {
            _fieldDefinition = fieldDefinition;
        }

        public string ServerItem
        {
            get { return _fieldDefinition.ServerItem; }
        }

        public string Name
        {
            get { return _fieldDefinition.Name; }
        }

        public bool Required
        {
            get { return _fieldDefinition.Required; }
        }

        public int DisplayOrder
        {
            get { return _fieldDefinition.DisplayOrder; }
        }
    }

    public class WrapperForPolicyFailure : WrapperFor<PolicyFailure>, IPolicyFailure
    {
        private readonly PolicyFailure _failure;

        public WrapperForPolicyFailure(PolicyFailure failure) : base(failure)
        {
            _failure = failure;
        }

        public string Message
        {
            get { return _failure.Message; }
        }
    }

    public class WrapperForWorkspace : WrapperFor<Workspace>, IWorkspace
    {
        private readonly TfsApiBridge _bridge;
        private readonly Workspace _workspace;

        public WrapperForWorkspace(TfsApiBridge bridge, Workspace workspace) : base(workspace)
        {
            _bridge = bridge;
            _workspace = workspace;
        }

        public IPendingChange[] GetPendingChanges()
        {
            return _bridge.Wrap<WrapperForPendingChange, PendingChange>(_workspace.GetPendingChanges());
        }

        public void Shelve(IShelveset shelveset, IPendingChange[] changes, TfsShelvingOptions options)
        {
            _workspace.Shelve(_bridge.Unwrap<Shelveset>(shelveset), _bridge.Unwrap<PendingChange>(changes), _bridge.Convert<ShelvingOptions>(options));
        }

        private PolicyOverrideInfo ToTfs(TfsPolicyOverrideInfo policyOverrideInfo)
        {
            if (policyOverrideInfo == null)
                return null;
            return new PolicyOverrideInfo(policyOverrideInfo.Comment,
                                          _bridge.Unwrap<PolicyFailure>(policyOverrideInfo.Failures));
        }

        public ICheckinEvaluationResult EvaluateCheckin(TfsCheckinEvaluationOptions options, IPendingChange[] allChanges, IPendingChange[] changes,
                                                        string comment, string author, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges)
        {
            return _bridge.Wrap<WrapperForCheckinEvaluationResult, CheckinEvaluationResult>(_workspace.EvaluateCheckin(
                _bridge.Convert<CheckinEvaluationOptions>(options),
                _bridge.Unwrap<PendingChange>(allChanges),
                _bridge.Unwrap<PendingChange>(changes),
                comment,
                _bridge.Unwrap<CheckinNote>(checkinNote),
                _bridge.Unwrap<WorkItemCheckinInfo>(workItemChanges)));
        }

        public int PendAdd(string path)
        {
            return _workspace.PendAdd(path);
        }

        public int PendEdit(string path)
        {
            return _workspace.PendEdit(new string[] { path }, RecursionType.None, null, LockLevel.Unchanged, false, PendChangesOptions.ForceCheckOutLocalVersion);
        }

        public int PendDelete(string path)
        {
            return _workspace.PendDelete(path);
        }

        public int PendRename(string pathFrom, string pathTo)
        {
            FileInfo info = new FileInfo(pathTo);
            if (info.Exists)
                info.Delete();
            return _workspace.PendRename(pathFrom, pathTo);
        }

        private void DoUntilNoFailures(Func<GetStatus> get)
        {
            Retry.DoWhile(() => get().NumFailures != 0);
        }

        public void ForceGetFile(string path, int changeset)
        {
            var item = new ItemSpec(path, RecursionType.None);
            DoUntilNoFailures(() => _workspace.Get(new GetRequest(item, changeset), GetOptions.Overwrite | GetOptions.GetAll));
        }

        public void GetSpecificVersion(int changeset)
        {
            Retry.Do(() => DoUntilNoFailures(() => _workspace.Get(new ChangesetVersionSpec(changeset), GetOptions.Overwrite | GetOptions.GetAll)));
        }

        public void GetSpecificVersion(int changesetId, IEnumerable<IItem> items, bool noParallel)
        {
            var version = new ChangesetVersionSpec(changesetId);
            GetRequests(items.Select(e => new GetRequest(new ItemSpec(e.ServerItem, RecursionType.Full), version)), noParallel);
        }

        public void GetSpecificVersion(IChangeset changeset, bool noParallel)
        {
            GetSpecificVersion(changeset.ChangesetId, changeset.Changes, noParallel);
        }

        public void GetSpecificVersion(int changesetId, IEnumerable<IChange> changes, bool noParallel)
        {
            GetRequests(changes.Select(change => new GetRequest(new ItemSpec(change.Item.ServerItem, RecursionType.None, change.Item.DeletionId), changesetId)), noParallel);
        }

        public string GetLocalItemForServerItem(string serverItem)
        {
            return _workspace.GetLocalItemForServerItem(serverItem);
        }

        public string GetServerItemForLocalItem(string localItem)
        {
            return _workspace.GetServerItemForLocalItem(localItem);
        }

        public string OwnerName
        {
            get { return _workspace.OwnerName; }
        }

        public void Merge(string sourceTfsPath, string targetTfsPath)
        {
            var status = _workspace.Merge(sourceTfsPath, targetTfsPath, null, null, LockLevel.None, RecursionType.Full,
                MergeOptions.AlwaysAcceptMine);
            var conflicts = _workspace.QueryConflicts(null, true);
            foreach (var conflict in conflicts)
            {
                conflict.Resolution = Resolution.AcceptYours;
                _workspace.ResolveConflict(conflict);
            }
        }

        public int Checkin(IPendingChange[] changes, string comment, string author, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges,
           TfsPolicyOverrideInfo policyOverrideInfo, bool overrideGatedCheckIn)
        {
            var checkinParameters = new WorkspaceCheckInParameters(_bridge.Unwrap<PendingChange>(changes), comment)
            {
                CheckinNotes = _bridge.Unwrap<CheckinNote>(checkinNote),
                AssociatedWorkItems = _bridge.Unwrap<WorkItemCheckinInfo>(workItemChanges),
                PolicyOverride = ToTfs(policyOverrideInfo),
                OverrideGatedCheckIn = overrideGatedCheckIn
            };

            if (author != null)
                checkinParameters.Author = author;

            try
            {
                return _workspace.CheckIn(checkinParameters);
            }
            catch (GatedCheckinException gatedException)
            {
                throw new GitTfsGatedCheckinException(gatedException.ShelvesetName, gatedException.AffectedBuildDefinitions, gatedException.CheckInTicket);
            }
        }

        public void GetRequests(IEnumerable<GetRequest> source, bool noParallel, int batchSize = 20)
        {

            source.ToBatch(batchSize).ForEach(batch =>
            {
                var items = batch;
                Retry.Do(() =>
                {
                    while (items.Length > 0)
                    {
                        var status = _workspace.Get(items.ToArray(), GetOptions.Overwrite | GetOptions.GetAll);
                        if (status.NumFailures == 0)
                        {
                            break;
                        }

                        items = status.GetFailures().Join(items, e => e.ServerItem, e => e.ItemSpec.Item, (failure, request) => request).ToArray();
                    }
                });
            }, !noParallel);
        }
    }

    public class WrapperForBranchObject : WrapperFor<BranchObject>, IBranchObject
    {
        private readonly BranchObject _branch;

        public WrapperForBranchObject(BranchObject branch)
            : base(branch)
        {
            _branch = branch;
        }

        public string Path
        {
            get { return _branch.Properties.RootItem.Item; }
        }

        public bool IsRoot
        {
            get { return _branch.Properties.ParentBranch == null; }
        }

        public string ParentPath
        {
            get { return _branch.Properties.ParentBranch.Item; }
        }
    }
}