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

        protected override string GetVsInstallDir()
        {
            return TryGetRegString(@"Software\Microsoft\VisualStudio\12.0", "InstallDir")
                ?? TryGetRegString(@"Software\WOW6432Node\Microsoft\VisualStudio\12.0", "InstallDir");
        }
    }
}
