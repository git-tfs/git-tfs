using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Setup.Configuration;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2017
{
    public class TfsHelper : TfsHelperBase
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        private const string privateAssembliesFolder =
            @"Common7\IDE\PrivateAssemblies";

        private const string tfsExtensionsFolder =
            @"Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer";

        protected string TfsVersionString { get { return "15.0"; } }

        public TfsHelper(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        { }

        protected override bool HasWorkItems(Changeset changeset)
        {
            return Retry.Do(() => changeset.AssociatedWorkItems.Length > 0);
        }

        protected override string GetVsInstallDir()
        {
            try
            {
                string visualStudioInstallationPath = null;

                var setupConfiguration = (ISetupConfiguration2)GetSetupConfiguration();
                var instancesEnumerator = setupConfiguration.EnumAllInstances();

                int fetched;
                var instances = new ISetupInstance[1];
                do
                {
                    instancesEnumerator.Next(1, instances, out fetched);
                    if (fetched > 0)
                    {
                        var instance = (ISetupInstance2)instances[0];
                        var state = instance.GetState();
                        if ((state & InstanceState.Local) == InstanceState.Local)
                        {
                            visualStudioInstallationPath = instance.GetInstallationPath();
                        }
                    }
                }
                while (fetched > 0 && !string.IsNullOrEmpty(visualStudioInstallationPath));

                return visualStudioInstallationPath;
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
            return HasCredentials ?
                new TfsTeamProjectCollection(uri, new TfsClientCredentials(new WindowsCredential(GetCredential()))) :
                TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
#pragma warning restore 618
        }


        protected override string GetDialogAssemblyPath()
        {
            return Path.Combine(GetVsInstallDir(), tfsExtensionsFolder, DialogAssemblyName + ".dll");
        }

        protected override Assembly LoadFromVsFolder(object sender, ResolveEventArgs args)
        {
            Trace.WriteLine("Looking for assembly " + args.Name + " ...");
            string folderPath = Path.Combine(GetVsInstallDir(), privateAssembliesFolder);

            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath))
            {
                Trace.WriteLine("... loading " + args.Name + " from " + assemblyPath);
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }

        private static ISetupConfiguration GetSetupConfiguration()
        {
            try
            {
                // Try to CoCreate the class object.
                return new SetupConfiguration();
            }
            catch (COMException ex)
            {
                if (ex.HResult == REGDB_E_CLASSNOTREG)
                {
                    // Attempt to get the class object using an app-local call.
                    ISetupConfiguration setupConfiguration;

                    var result = GetSetupConfiguration(out setupConfiguration, IntPtr.Zero);
                    if (result < 0)
                    {
                        throw new COMException("Failed to get setup configuration query.", result);
                    }

                    return setupConfiguration;
                }

                throw ex;
            }
        }

        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll", ExactSpelling = true, PreserveSig = true)]
        static extern int GetSetupConfiguration([MarshalAs(UnmanagedType.Interface), Out] out ISetupConfiguration configuration, IntPtr reserved);
    }
}
