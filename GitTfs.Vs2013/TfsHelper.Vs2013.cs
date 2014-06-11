using System.IO;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2013
{
    public class TfsHelper : TfsHelperVs2012Base
    {
        public TfsHelper(TextWriter stdout, TfsApiBridge bridge, IContainer container) : base(stdout, bridge, container)
        {
        }

        private string vsInstallDir;

        protected override string GetVsInstallDir()
        {
            if (vsInstallDir == null)
            {
                vsInstallDir = TryGetRegString(@"Software\Microsoft\VisualStudio\12.0", "InstallDir")
                ?? TryGetRegString(@"Software\WOW6432Node\Microsoft\VisualStudio\12.0", "InstallDir")
                ?? TryGetUserRegString(@"Software\Microsoft\WDExpress\12.0_Config", "InstallDir")
                ?? TryGetUserRegString(@"Software\WOW6432Node\Microsoft\WDExpress\12.0_Config", "InstallDir");
            }
            return vsInstallDir;
        }
    }
}
