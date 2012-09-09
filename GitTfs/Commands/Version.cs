using System;
using System.ComponentModel;
using System.IO;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("version")]
    [Description("version")]
    public class Version : GitTfsCommand
    {
        private Globals globals;
        TextWriter stdout;
        IGitTfsVersionProvider versionProvider;

        /// <summary>
        /// Initializes a new instance of the Version class.
        /// </summary>
        /// <param name="stdout"></param>
        /// <param name="versionProvider"></param>
        public Version(Globals globals, TextWriter stdout, IGitTfsVersionProvider versionProvider)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.versionProvider = versionProvider;

            this.OptionSet = globals.OptionSet;
        }

        public int Run()
        {
            stdout.WriteLine(versionProvider.GetVersionString());
            stdout.WriteLine(versionProvider.GetPathToGitTfsExecutable());

            return GitTfsExitCodes.OK;
        }

        public OptionSet OptionSet { get; private set; }
    }
}
