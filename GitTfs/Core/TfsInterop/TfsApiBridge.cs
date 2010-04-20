using System;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using ChangeType=Microsoft.TeamFoundation.VersionControl.Client.ChangeType;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public class TfsApiBridge
    {
        public IChangeset Wrap(Changeset changeset)
        {
            return new NeedsToBeReflectedChangeset(this, changeset);
        }

        public IChange Wrap(Change change)
        {
            return new NeedsToBeReflectedChange(this, change);
        }

        public IItem Wrap(Item item)
        {
            return new NeedsToBeReflectedItem(this, item);
        }

        public IIdentity Wrap(Identity identity)
        {
            return identity == null ? (IIdentity)new NullIdentity() : new NeedsToBeReflectedIdentity(identity);
        }

        public TfsChangeType Convert(ChangeType type)
        {
            return (TfsChangeType) (int) type;
        }

        public TfsItemType Convert(ItemType type)
        {
            return (TfsItemType) (int) type;
        }
    }
}