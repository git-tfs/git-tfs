using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    // These things should be auto-generated, or reflected, or duck-typed, or something to break the explicit dependence on the TF dlls.
    class NeedsToBeReflectedChangeset : WrapperFor<Changeset>, IChangeset
    {
        private readonly TfsApiBridge _bridge;
        private readonly Changeset _changeset;

        public NeedsToBeReflectedChangeset(TfsApiBridge bridge, Changeset changeset) : base(changeset)
        {
            _bridge = bridge;
            _changeset = changeset;
        }

        public IEnumerable<IChange> Changes
        {
            get { return _changeset.Changes.Select(c => _bridge.Wrap(c)); }
        }

        public string Committer
        {
            get { return _changeset.Committer; }
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
    }

    class NeedsToBeReflectedChange : WrapperFor<Change>, IChange
    {
        private readonly TfsApiBridge _bridge;
        private readonly Change _change;

        public NeedsToBeReflectedChange(TfsApiBridge bridge, Change change) : base(change)
        {
            _bridge = bridge;
            _change = change;
        }

        public TfsChangeType ChangeType
        {
            get { return _bridge.Convert(_change.ChangeType); }
        }

        public IItem Item
        {
            get { return _bridge.Wrap(_change.Item); }
        }
    }

    class NeedsToBeReflectedItem : WrapperFor<Item>, IItem
    {
        private readonly TfsApiBridge _bridge;
        private readonly Item _item;

        public NeedsToBeReflectedItem(TfsApiBridge bridge, Item item) : base(item)
        {
            _bridge = bridge;
            _item = item;
        }

        public IItem GetVersion(int changeset)
        {
            return _bridge.Wrap(_item.VersionControlServer.GetItem(_item.ItemId, _item.ChangesetId - 1));
        }

        public int ChangesetId
        {
            get { return _item.ChangesetId; }
        }

        public string ServerItem
        {
            get { return _item.ServerItem; }
        }

        public decimal DeletionId
        {
            get { return _item.DeletionId; }
        }

        public TfsItemType ItemType
        {
            get { return _bridge.Convert(_item.ItemType); }
        }

        public void DownloadFile(string file)
        {
            _item.DownloadFile(file);
        }
    }

    class NeedsToBeReflectedIdentity : WrapperFor<Identity>, IIdentity
    {
        private readonly Identity _identity;

        public NeedsToBeReflectedIdentity(Identity identity) : base(identity)
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

    class NeedsToBeReflectedShelveset : WrapperFor<Shelveset>, IShelveset
    {
        private readonly Shelveset _shelveset;
        private readonly TfsApiBridge _bridge;

        public NeedsToBeReflectedShelveset(TfsApiBridge bridge, Shelveset shelveset) : base(shelveset)
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
            get { return _shelveset.WorkItemInfo.Select(i => _bridge.Wrap(i)).ToArray(); }
            set { _shelveset.WorkItemInfo = value.Select(i => _bridge.Unwrap(i)).ToArray(); }
        }
    }

    class NeedsToBeReflectedWorkItemCheckinInfo : WrapperFor<WorkItemCheckinInfo>, IWorkItemCheckinInfo
    {
        private readonly WorkItemCheckinInfo _info;

        public NeedsToBeReflectedWorkItemCheckinInfo(WorkItemCheckinInfo info) : base(info)
        {
            _info = info;
        }
    }

    class NeedsToBeReflectedPendingChange : WrapperFor<PendingChange>, IPendingChange
    {
        private readonly PendingChange _pendingChange;

        public NeedsToBeReflectedPendingChange(PendingChange pendingChange) : base(pendingChange)
        {
            _pendingChange = pendingChange;
        }
    }

    class NeedsToBeReflectedWorkspace : WrapperFor<Workspace>, IWorkspace
    {
        private readonly TfsApiBridge _bridge;
        private readonly Workspace _workspace;

        public NeedsToBeReflectedWorkspace(TfsApiBridge bridge, Workspace workspace) : base(workspace)
        {
            _bridge = bridge;
            _workspace = workspace;
        }

        public IEnumerable<IPendingChange> GetPendingChanges()
        {
            return _workspace.GetPendingChanges().Select(c => _bridge.Wrap(c));
        }

        public void Shelve(IShelveset shelveset, IEnumerable<IPendingChange> changes, TfsShelvingOptions options)
        {
            _workspace.Shelve(_bridge.Unwrap(shelveset), changes.Select(c => _bridge.Unwrap(c)).ToArray(), _bridge.Convert(options));
        }

        public int PendAdd(string path)
        {
            return _workspace.PendAdd(path);
        }

        public int PendEdit(string path)
        {
            return _workspace.PendEdit(path);
        }

        public int PendDelete(string path)
        {
            return _workspace.PendDelete(path);
        }

        public void ForceGetFile(string path, int changeset)
        {
            var item = new ItemSpec(path, RecursionType.None);
            _workspace.Get(new GetRequest(item, changeset), GetOptions.Overwrite | GetOptions.GetAll);
        }

        public string OwnerName
        {
            get { return _workspace.OwnerName; }
        }
    }
}