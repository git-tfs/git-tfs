using System;
using System.ComponentModel;
using System.IO;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("info")]
    [Description("info")]
    [RequiresValidGitRepository]
    public class Info : GitTfsCommand
    {
        Globals globals;
        TextWriter stdout;
        IGitHelpers githelpers;
        IHelpHelper help;

        /// <summary>
        /// Initializes a new instance of the Info class.
        /// </summary>
        /// <param name="globals"></param>
        /// <param name="stdout"></param>
        /// <param name="githelpers"></param>
        /// <param name="help"></param>
        public Info(Globals globals, TextWriter stdout, IGitHelpers githelpers, IHelpHelper help)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.githelpers = githelpers;
            this.help = help;
        }

        public OptionSet OptionSet { get { return globals.OptionSet; } }

        public int Run()
        {
            stdout.WriteLine("remote tfs id: {0}", globals.RemoteId);
            foreach (var changeset in globals.Repository.GetLastParentTfsCommits("HEAD"))
            {
                stdout.WriteLine("- {0} {1}", changeset.Remote.TfsUrl, changeset.Remote.TfsRepositoryPath);
            }

            return GitTfsExitCodes.OK;
        }
    }
}
