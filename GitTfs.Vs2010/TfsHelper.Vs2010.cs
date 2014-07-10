using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2010
{
    public class TfsHelper : TfsHelperBase
    {
        public TfsHelper(TextWriter stdout, TfsApiBridge bridge, IContainer container) : base(stdout, bridge, container)
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
    }
}
