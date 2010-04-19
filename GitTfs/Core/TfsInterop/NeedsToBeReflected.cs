using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    // These things should be auto-generated, or reflected, or duck-typed, or something to break the explicit dependence on the TF dlls.
    public class NeedsToBeReflectedChangeset : IChangeset
    {
        private readonly TfsApiBridge _bridge;
        private readonly Changeset _changeset;

        public NeedsToBeReflectedChangeset(TfsApiBridge bridge, Changeset changeset)
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

    public class NeedsToBeReflectedChange : IChange
    {
        private readonly TfsApiBridge _bridge;
        private readonly Change _change;

        public NeedsToBeReflectedChange(TfsApiBridge bridge, Change change)
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

    public class NeedsToBeReflectedItem : IItem
    {
        private readonly TfsApiBridge _bridge;
        private readonly Item _item;

        public NeedsToBeReflectedItem(TfsApiBridge bridge, Item item)
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
}