using System;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using StructureMap;
using Sep.Git.Tfs.Core.TfsInterop;
using Microsoft.TeamFoundation.Build.Client;

namespace Sep.Git.Tfs.VsCommon
{
    public abstract class TfsHelperVs2012Base : TfsHelperBase
    {
        protected abstract string TfsVersionString { get; }
        protected TfsHelperVs2012Base(TextWriter stdout, TfsApiBridge bridge, IContainer container)
            : base(stdout, bridge, container) { }

        protected override bool HasWorkItems(Changeset changeset)
        {
            return Retry.Do(() => changeset.AssociatedWorkItems.Length > 0);
        }

        private string vsInstallDir;
        protected override string GetVsInstallDir()
        {
            if (vsInstallDir == null)
            {
                vsInstallDir = TryGetRegString(@"Software\WOW6432Node\Microsoft\VisualStudio\" + TfsVersionString, "InstallDir")
                    ?? TryGetRegString(@"Software\Microsoft\VisualStudio\" + TfsVersionString, "InstallDir")
                    ?? TryGetUserRegString(@"Software\WOW6432Node\Microsoft\WDExpress\" + TfsVersionString + "_Config", "InstallDir")
                    ?? TryGetUserRegString(@"Software\Microsoft\WDExpress\" + TfsVersionString + "_Config", "InstallDir");
            }
            return vsInstallDir;
        }

        protected override IBuildDetail GetSpecificBuildFromQueuedBuild(IQueuedBuild queuedBuild, string shelvesetName)
        {
            var build = queuedBuild.Builds.FirstOrDefault(b => b.ShelvesetName == shelvesetName);
            return build != null ? build : queuedBuild.Build;
        }

        private IIdentityManagementService IdentityManagementService
        {
            get { return GetService<IIdentityManagementService>(); }
        }

        public override IIdentity GetIdentity(string username)
        {
            var identities = new Guid[] { new Guid(username) };
            var teamFoundationIdentities = Retry.Do(() => IdentityManagementService.ReadIdentities(identities, MembershipQuery.None));
            var teamFoundationIdentity = teamFoundationIdentities.First();
            return _bridge.Wrap<WrapperForIdentity, TeamFoundationIdentity>(teamFoundationIdentity);
        }

        protected override TfsTeamProjectCollection GetTfsCredential(Uri uri)
        {
#pragma warning disable 618
            return HasCredentials ?
                new TfsTeamProjectCollection(uri, GetCredential(), new UICredentialsProvider()) :
                new TfsTeamProjectCollection(uri, new UICredentialsProvider());
#pragma warning restore 618
        }
    }
}
