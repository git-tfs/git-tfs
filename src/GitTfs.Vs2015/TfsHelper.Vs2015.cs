using System;
using System.Diagnostics;
using System.IO;
using GitTfs.VsCommon;
using StructureMap;

namespace GitTfs.Vs2015
{
    public class TfsHelper : TfsHelperVs2012Base
    {
        protected override string TfsVersionString { get { return "14.0"; } }

        public TfsHelper(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        { }

        protected override string GetDialogAssemblyPath()
        {
#if NETFRAMEWORK
            var tfsExtensionsFolder = TryGetUserRegStringStartingWithName(@"Software\Microsoft\VisualStudio\14.0\ExtensionManager\EnabledExtensions", "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions");
            return Path.Combine(tfsExtensionsFolder, DialogAssemblyName + ".dll");
#else
            Trace.TraceWarning("Checkin dialog is not supported with dotnet core version of git-tfs");
            return string.Empty;
#endif
        }
    }
}
