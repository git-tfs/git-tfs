using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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
        private const string myPrivateAssembliesFolder =
            @"Common7\IDE\PrivateAssemblies";

        private const string myTeamExplorerFolder =
            @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer";

        private readonly List<string> myAssemblySearchPaths;

        /// <summary>
        /// Caches the found VS installation path.
        /// </summary>
        private string myVisualStudioInstallationPath;

        public TfsHelperVS2017Base(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        {
            myVisualStudioInstallationPath = GetVsInstallDir();

            myAssemblySearchPaths = new List<string>();
            if (!string.IsNullOrEmpty(myVisualStudioInstallationPath))
            {
                myAssemblySearchPaths.Add(Path.Combine(myVisualStudioInstallationPath, myPrivateAssembliesFolder));
            }
        }

        protected override bool HasWorkItems(Changeset changeset)
        {
            return Retry.Do(() => changeset.AssociatedWorkItems.Length > 0);
        }

        /// <summary>
        /// Enumerates the list of installed VS instances and returns the first one
        /// matching <see cref="MajorVersion"/>. Right now there is no way for the user to influence
        /// which version to choose if multiple installed version have the same major,
        /// e.g. VS2017 installed as Enterprise and Professional.
        /// </summary>
        /// <returns>
        /// Path to the top level directory of the Visual studio installation directory,
        /// e.g. <c>C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise</c>
        /// </returns>
        protected string GetVsInstallDir()
        {
            return @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise";
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

        protected override Assembly LoadFromVsFolder(object sender, ResolveEventArgs args)
        {
            Trace.WriteLine("Looking for assembly " + args.Name + " ...");
            foreach (var dir in myAssemblySearchPaths)
            {
                string assemblyPath = Path.Combine(dir, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    Trace.WriteLine("... loading " + args.Name + " from " + assemblyPath);
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            return null;
        }
    }
}
