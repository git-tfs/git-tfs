using System.ComponentModel;
using System.Diagnostics;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("shelve-delete")]
    [Description("shelve-delete shelveset-name")]
    [RequiresValidGitRepository]
    public class ShelveDelete : GitTfsCommand
    {
        private readonly Globals _globals;

        public ShelveDelete(Globals globals)
        {
            _globals = globals;
            OptionSet = new OptionSet();
        }

        public OptionSet OptionSet { get; private set; }

        public int Run(string shelvesetName)
        {
            if (string.IsNullOrEmpty(shelvesetName))
            {
                Trace.TraceError("error: no shelveset name specified...");
                return GitTfsExitCodes.InvalidArguments;
            }

            var remote = _globals.Repository.ReadTfsRemote(_globals.RemoteId);
            if (!remote.HasShelveset(shelvesetName))
            {
                Trace.TraceInformation("error: could not find shelveset \"{0}\"...", shelvesetName);
                return GitTfsExitCodes.InvalidArguments;
            }

            remote.DeleteShelveset(shelvesetName);
            Trace.TraceInformation("Shelveset \"{0}\" deleted.", shelvesetName);
            return GitTfsExitCodes.OK;
        }
    }
}