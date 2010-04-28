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

        public IWorkspace Wrap(Workspace workspace)
        {
            return new NeedsToBeReflectedWorkspace(this, workspace);
        }

        public Workspace Unwrap(IWorkspace workspace)
        {
            return WrapperFor<Workspace>.Unwrap(workspace);
        }

        public IShelveset Wrap(Shelveset shelveset)
        {
            return new NeedsToBeReflectedShelveset(this, shelveset);
        }

        public Shelveset Unwrap(IShelveset shelveset)
        {
            return WrapperFor<Shelveset>.Unwrap(shelveset);
        }

        public IPendingChange Wrap(PendingChange pendingChange)
        {
            return new NeedsToBeReflectedPendingChange(pendingChange);
        }

        public PendingChange Unwrap(IPendingChange pendingChange)
        {
            return WrapperFor<PendingChange>.Unwrap(pendingChange);
        }

        public IWorkItemCheckinInfo Wrap(WorkItemCheckinInfo workItemCheckinInfo)
        {
            return new NeedsToBeReflectedWorkItemCheckinInfo(workItemCheckinInfo);
        }

        public IVersionControlServer Wrap(VersionControlServer versionControlServer)
        {
            return new NeedsToBeReflectedVersionControlServer(this, versionControlServer);
        }

        public WorkItemCheckinInfo Unwrap(IWorkItemCheckinInfo info)
        {
            return WrapperFor<WorkItemCheckinInfo>.Unwrap(info);
        }

        public TfsChangeType Convert(ChangeType type)
        {
            return (TfsChangeType) (int) type;
        }

        public TfsItemType Convert(ItemType type)
        {
            return (TfsItemType) (int) type;
        }

        public WorkItemCheckinAction Convert(TfsWorkItemCheckinAction action)
        {
            return (WorkItemCheckinAction) (int) action;
        }

        public ShelvingOptions Convert(TfsShelvingOptions options)
        {
            return (ShelvingOptions) (int) options;
        }
    }
}