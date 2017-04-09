using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Sep.Git.Tfs.VsCommon;
using StructureMap;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Vs2010
{
    public class TfsHelper : TfsHelperBase
    {
        public TfsHelper(TfsApiBridge bridge, IContainer container) : base(bridge, container)
        {
        }

        private string vsInstallDir;
        protected override string GetVsInstallDir()
        {
            if (vsInstallDir == null)
            {
                vsInstallDir = TryGetRegString(@"Software\Microsoft\VisualStudio\10.0", "InstallDir")
                    ?? TryGetRegString(@"Software\WOW6432Node\Microsoft\VisualStudio\10.0", "InstallDir");
            }
            return vsInstallDir;
        }

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
                    new TfsTeamProjectCollection(uri, GetCredential(), new UICredentialsProvider()) :
                    new TfsTeamProjectCollection(uri, new UICredentialsProvider());
        }

        protected override Assembly LoadFromVsFolder(object sender, ResolveEventArgs args)
        {
            Trace.WriteLine("Looking for assembly " + args.Name + " ...");
            string folderPath = Path.Combine(GetVsInstallDir(), "PrivateAssemblies");
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false)
                return null;
            Trace.WriteLine("... loading " + args.Name + " from " + assemblyPath);
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}
