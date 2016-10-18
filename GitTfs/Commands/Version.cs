using System;
using System.ComponentModel;
using System.IO;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;
using System.Diagnostics;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("version")]
    [Description("version")]
    public class Version : GitTfsCommand
    {
        private Globals globals;
        private IGitTfsVersionProvider versionProvider;

        /// <summary>
        /// Initializes a new instance of the Version class.
        /// </summary>
        /// <param name="stdout"></param>
        /// <param name="versionProvider"></param>
        public Version(Globals globals, IGitTfsVersionProvider versionProvider)
        {
            this.globals = globals;
            this.versionProvider = versionProvider;

            this.OptionSet = globals.OptionSet;
        }

        public int Run()
        {
            Trace.TraceInformation(versionProvider.GetVersionString());
            Trace.TraceInformation(versionProvider.GetPathToGitTfsExecutable());

            Trace.TraceInformation(GitTfsConstants.MessageForceVersion);

            return GitTfsExitCodes.OK;
        }

        public OptionSet OptionSet { get; private set; }
    }
}
