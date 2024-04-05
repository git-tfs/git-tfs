using System.ComponentModel;
using NDesk.Options;
using GitTfs.Core;
using StructureMap;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("cleanup-workspaces")]
    [Description("cleanup-workspaces [tfs-remote-id]...")]
    [RequiresValidGitRepository]
    public class CleanupWorkspaces : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly CleanupOptions _cleanupOptions;

        public CleanupWorkspaces(Globals globals, CleanupOptions cleanupOptions)
        {
            _globals = globals;
            _cleanupOptions = cleanupOptions;
        }

        public OptionSet OptionSet => _cleanupOptions.OptionSet;

        public int Run()
        {
            _cleanupOptions.Init();
            foreach (var remote in _globals.Repository.ReadAllTfsRemotes())
            {
                Cleanup(remote);
            }
            return GitTfsExitCodes.OK;
        }

        public int Run(IList<string> remoteIds)
        {
            _cleanupOptions.Init();
            foreach (var remoteId in remoteIds)
            {
                var remote = _globals.Repository.ReadTfsRemote(remoteId);
                Cleanup(remote);
            }
            return GitTfsExitCodes.OK;
        }

        private void Cleanup(IGitTfsRemote remote)
        {
            Trace.TraceInformation("Cleaning up workspaces for TFS remote " + remote.Id);
            remote.CleanupWorkspace();
        }
    }
}
