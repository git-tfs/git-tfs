using GitTfs.Core.TfsInterop;

namespace GitTfs.Core
{
    public class CheckinPolicyEvaluator
    {
        public CheckinPolicyEvaluationResult EvaluateCheckin(IWorkspace workspace, IPendingChange[] pendingChanges, string comment, ICheckinNote checkinNote, IEnumerable<IWorkItemCheckinInfo> workItemInfo)
        {
            var result = workspace.EvaluateCheckin(TfsCheckinEvaluationOptions.All, pendingChanges,
                                                   pendingChanges, comment, null, checkinNote,
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

            public bool HasErrors => Messages.Any();

            public IEnumerable<string> Messages => BuildMessages();

            public ICheckinEvaluationResult Result => _result;

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
