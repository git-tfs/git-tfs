using System.Diagnostics;
using Microsoft.TeamFoundation.Framework.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public class WrapperForTeamFoundationIdentity : WrapperFor<TeamFoundationIdentity>, IIdentity
    {
        private readonly TeamFoundationIdentity _identity;

        public WrapperForTeamFoundationIdentity(TeamFoundationIdentity identity)
            : base(identity)
        {
            Debug.Assert(identity != null, "wrapped property must not be null.");
            _identity = identity;
        }

        public string MailAddress
        {
            get { return _identity.UniqueName; } // when tested with VSTS this seems to be the property that email lives in
        }

        public string DisplayName
        {
            get { return _identity.DisplayName; }
        }
    }
}