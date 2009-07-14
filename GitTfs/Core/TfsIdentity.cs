using System;
using Microsoft.TeamFoundation.Server;

namespace Sep.Git.Tfs.Core
{
    class TfsIdentity : ITfsIdentity
    {
        private readonly Identity identity;

        public TfsIdentity(Identity identity)
        {
            this.identity = identity;
        }

        public string MailAddress
        {
            get { return identity.MailAddress; }
        }

        public string DisplayName
        {
            get { return identity.DisplayName; }
        }
    }
}
