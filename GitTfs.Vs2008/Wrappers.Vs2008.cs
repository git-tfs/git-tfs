using System.Collections.Generic;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public partial class WrapperForWorkspace
    {
        public int Checkin(IPendingChange[] changes, string comment, string author, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemChanges,
                           TfsPolicyOverrideInfo policyOverrideInfo, bool overrideGatedCheckIn)
        {
            if (author != null)
            {
                return _workspace.CheckIn(
                    _bridge.Unwrap<PendingChange>(changes),
                    comment,
                    author,
                    _bridge.Unwrap<CheckinNote>(checkinNote),
                    _bridge.Unwrap<WorkItemCheckinInfo>(workItemChanges),
                    ToTfs(policyOverrideInfo));
            }

            return _workspace.CheckIn(
                _bridge.Unwrap<PendingChange>(changes),
                comment,
                _bridge.Unwrap<CheckinNote>(checkinNote),
                _bridge.Unwrap<WorkItemCheckinInfo>(workItemChanges),
                ToTfs(policyOverrideInfo));
        }


    }
}
