using System.IO;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2012
{
    public class TfsHelper : TfsHelperVs2012Base
    {
        public TfsHelper(TextWriter stdout, TfsApiBridge bridge, IContainer container) : base(stdout, bridge, container)
        {
        }

        protected override string GetVsInstallDir()
        {
            return TryGetRegString(@"Software\Microsoft\VisualStudio\11.0", "InstallDir")
                ?? TryGetRegString(@"Software\WOW6432Node\Microsoft\VisualStudio\11.0", "InstallDir")
                ?? TryGetUserRegString(@"Software\Microsoft\WDExpress\11.0_Config", "InstallDir")
                ?? TryGetUserRegString(@"Software\WOW6432Node\Microsoft\WDExpress\11.0_Config", "InstallDir");
        }
    }
}
