using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("cleanup-workspaces")]
    [Description("cleanup-workspaces [tfs-remote-id]...")]
    [RequiresValidGitRepository]
    public class CleanupWorkspaces : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly Globals _globals;
        private readonly CleanupOptions _cleanupOptions;

        public CleanupWorkspaces(TextWriter stdout, Globals globals, CleanupOptions cleanupOptions)
        {
            _stdout = stdout;
            _globals = globals;
            _cleanupOptions = cleanupOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(_cleanupOptions); }
        }

        public int Run()
        {
            _cleanupOptions.Init();
            foreach(var remote in _globals.Repository.ReadAllTfsRemotes())
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
            _stdout.WriteLine("Cleaning up workspaces for TFS remote " + remote.Id);
            remote.CleanupWorkspace();
        }
    }
}
