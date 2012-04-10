using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public partial class WrapperForWorkspace
    {
        public int Checkin(IPendingChange[] changes, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges,
                           TfsPolicyOverrideInfo policyOverrideInfo, bool overrideGatedCheckIn)
        {
            var checkinParameters = new WorkspaceCheckInParameters(_bridge.Unwrap<PendingChange>(changes), comment)
            {
                CheckinNotes = _bridge.Unwrap<CheckinNote>(checkinNote),
                AssociatedWorkItems = _bridge.Unwrap<WorkItemCheckinInfo>(workItemChanges),
                PolicyOverride = ToTfs(policyOverrideInfo),
                OverrideGatedCheckIn = overrideGatedCheckIn
            };

            return _workspace.CheckIn(checkinParameters);
        }
    }
}
