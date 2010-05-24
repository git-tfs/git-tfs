using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Vs2008;
using ChangeType=Microsoft.TeamFoundation.VersionControl.Client.ChangeType;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public class TfsApiBridge
    {
        public IChangeset Wrap(Changeset changeset)
        {
            return new WrapperForChangeset(this, changeset);
        }

        public IChange Wrap(Change change)
        {
            return new WrapperForChange(this, change);
        }

        public IItem Wrap(Item item)
        {
            return new WrapperForItem(this, item);
        }

        public IIdentity Wrap(Identity identity)
        {
            return identity == null ? (IIdentity)new NullIdentity() : new WrapperForIdentity(identity);
        }

        public IWorkspace Wrap(Workspace workspace)
        {
            return new WrapperForWorkspace(this, workspace);
        }

        public Workspace Unwrap(IWorkspace workspace)
        {
            return WrapperFor<Workspace>.Unwrap(workspace);
        }

        public IShelveset Wrap(Shelveset shelveset)
        {
            return new WrapperForShelveset(this, shelveset);
        }

        public Shelveset Unwrap(IShelveset shelveset)
        {
            return WrapperFor<Shelveset>.Unwrap(shelveset);
        }

        public IPendingChange Wrap(PendingChange pendingChange)
        {
            return new WrapperForPendingChange(pendingChange);
        }

        public PendingChange Unwrap(IPendingChange pendingChange)
        {
            return WrapperFor<PendingChange>.Unwrap(pendingChange);
        }

        public IWorkItemCheckinInfo Wrap(WorkItemCheckinInfo workItemCheckinInfo)
        {
            return new WrapperForWorkItemCheckinInfo(workItemCheckinInfo);
        }

        public IVersionControlServer Wrap(VersionControlServer versionControlServer)
        {
            return new WrapperForVersionControlServer(this, versionControlServer);
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