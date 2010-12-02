using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class CheckinPolicyEvaluator
    {
        public IEnumerable<string> EvaluateCheckin(IWorkspace workspace, IShelveset shelveset, IPendingChange[] pendingChanges)
        {
            var result = workspace.EvaluateCheckin(TfsCheckinEvaluationOptions.All, pendingChanges,
                                                   pendingChanges, shelveset.Comment, null,
                                                   shelveset.WorkItemInfo);
            return BuildMessages(result);
        }

        private IEnumerable<string> BuildMessages(ICheckinEvaluationResult result)
        {
            foreach (var x in result.Conflicts)
            {
                yield return "Conflict: " + x.ServerItem + ": " + x.Message;
            }
            foreach (var x in result.PolicyFailures)
            {
                yield return "Policy: " + x.Message;
            }
            foreach (var x in result.NoteFailures)
            {
                yield return "Checkin Note: " + x.Definition.Name + ": " + x.Message;
            }
            if (result.PolicyEvaluationException != null)
            {
                yield return "Exception: " + result.PolicyEvaluationException.Message;
            }
        }
    }
}
