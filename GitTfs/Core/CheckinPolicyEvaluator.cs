using System.Collections.Generic;
using System.Linq;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class CheckinPolicyEvaluator
    {
        public CheckinPolicyEvaluationResult EvaluateCheckin(IWorkspace workspace, IPendingChange[] pendingChanges, string comment, IEnumerable<IWorkItemCheckinInfo> workItemInfo)
        {
            var result = workspace.EvaluateCheckin(TfsCheckinEvaluationOptions.All, pendingChanges,
                                                   pendingChanges, comment, null,
                                                   workItemInfo);
            return new CheckinPolicyEvaluationResult(result);
        }

        public class CheckinPolicyEvaluationResult
        {
            private readonly ICheckinEvaluationResult _result;

            public CheckinPolicyEvaluationResult(ICheckinEvaluationResult result)
            {
                _result = result;
            }

            public bool HasErrors
            {
                get { return Messages.Any(); }
            }

            public IEnumerable<string> Messages
            {
                get { return BuildMessages(); }
            }

            public ICheckinEvaluationResult Result
            {
                get { return _result; }
            }

            private IEnumerable<string> BuildMessages()
            {
                foreach (var x in _result.Conflicts)
                {
                    yield return "Conflict: " + x.ServerItem + ": " + x.Message;
                }
                foreach (var x in _result.PolicyFailures)
                {
                    yield return "Policy: " + x.Message;
                }
                foreach (var x in _result.NoteFailures)
                {
                    yield return "Checkin Note: " + x.Definition.Name + ": " + x.Message;
                }
                if (_result.PolicyEvaluationException != null)
                {
                    yield return "Exception: " + _result.PolicyEvaluationException.Message;
                }
            }
        }
    }
}
