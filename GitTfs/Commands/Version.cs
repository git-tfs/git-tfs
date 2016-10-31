using System.ComponentModel;
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
        private readonly IGitTfsVersionProvider _versionProvider;

        /// <summary>
        /// Initializes a new instance of the Version class.
        /// </summary>
        /// <param name="globals"></param>
        /// <param name="versionProvider"></param>
        public Version(Globals globals, IGitTfsVersionProvider versionProvider)
        {
            _versionProvider = versionProvider;
            OptionSet = globals.OptionSet;
        }

        public int Run()
        {
            Trace.TraceInformation(_versionProvider.GetVersionString());
            Trace.TraceInformation(_versionProvider.GetPathToGitTfsExecutable());

            Trace.TraceInformation(GitTfsConstants.MessageForceVersion);

            return GitTfsExitCodes.OK;
        }

        public OptionSet OptionSet { get; private set; }
    }
}
