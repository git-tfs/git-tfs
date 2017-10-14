using System;
using System.ComponentModel;
using System.Diagnostics;
using NDesk.Options;
using StructureMap;
using GitTfs.Core;

namespace GitTfs.Commands
{
    [Pluggable("unshelve")]
    [Description("unshelve [options] shelve-name destination-branch")]
    [RequiresValidGitRepository]
    public class Unshelve : GitTfsCommand
    {
        private readonly Globals _globals;

        public Unshelve(Globals globals)
        {
            _globals = globals;
            TfsBranch = null;
        }

        public string Owner { get; set; }
        public string TfsBranch { get; set; }
        public bool Force { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "u|user=", "Shelveset owner (default: current user)\nUse 'all' to search all shelvesets.",
                        v => Owner = v },
                    { "b|branch=", "Git Branch to apply Shelveset to? (default: TFS current remote)",
                        v => TfsBranch = v },
                    { "force", "Get as much of the Shelveset as possible, and log any other errors",
                        v => Force = v != null },
                };
            }
        }

        public int Run(string shelvesetName, string destinationBranch)
        {
            if (string.IsNullOrEmpty(TfsBranch))//If destination not on command line, set up defaults.
                TfsBranch = _globals.RemoteId;

            var remote = _globals.Repository.ReadTfsRemote(TfsBranch);
            remote.Unshelve(Owner, shelvesetName, destinationBranch, BuildErrorHandler(), Force);
            Trace.TraceInformation("Created branch " + destinationBranch + " from shelveset \"" + shelvesetName + "\".");
            return GitTfsExitCodes.OK;
        }

        private Action<Exception> BuildErrorHandler()
        {
            if (Force)
            {
                return (e) =>
                {
                    Trace.TraceWarning("WARNING: unshelve: " + e.Message);
                    Trace.WriteLine(e);
                };
            }
            return null;
        }
    }
}
