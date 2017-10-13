using System.IO;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2015
{
    public class TfsHelper : TfsHelperVs2012Base
    {
        protected override string TfsVersionString { get { return "14.0"; } }

        public TfsHelper(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        { }

        protected override string GetDialogAssemblyPath()
        {
            var tfsExtensionsFolder = TryGetUserRegStringStartingWithName(@"Software\Microsoft\VisualStudio\14.0\ExtensionManager\EnabledExtensions", "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions");
            return Path.Combine(tfsExtensionsFolder, DialogAssemblyName + ".dll");
        }
    }
}
