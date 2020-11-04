using System;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using StructureMap;
using GitTfs.Core.TfsInterop;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualStudio.Services.Client;

namespace GitTfs.VsCommon
{
    public abstract class TfsHelperVS2017Base : TfsHelperBase
    {
        private const string myTeamExplorerFolder =
            @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer";

        protected abstract string TfsVersionString { get; }
        public TfsHelperVS2017Base(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        { }

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

#pragma warning disable 618
        private IGroupSecurityService GroupSecurityService
        {
            get { return GetService<IGroupSecurityService>(); }
        }

        public override IIdentity GetIdentity(string username)
        {
            return _bridge.Wrap<WrapperForIdentity, Identity>(Retry.Do(() => GroupSecurityService.ReadIdentity(SearchFactor.AccountName, username, QueryMembership.None)));
        }

        protected override TfsTeamProjectCollection GetTfsCredential(Uri uri)
        {
            var winCred = HasCredentials ?
                              new Microsoft.VisualStudio.Services.Common.WindowsCredential(GetCredential()) :
                              new Microsoft.VisualStudio.Services.Common.WindowsCredential(true);
            var vssCred = new VssClientCredentials(winCred);

            return new TfsTeamProjectCollection(uri, vssCred);
#pragma warning restore 618
        }

        protected override string GetDialogAssemblyPath()
        {
            return Path.Combine(GetVsInstallDir(), myTeamExplorerFolder, DialogAssemblyName + ".dll");
        }
    }
}
