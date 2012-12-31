﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("cleanup-workspace-local")]
    [Description("cleanup-workspace-local [tfs-remote-id]...")]
    [RequiresValidGitRepository]
    public class CleanupWorkspaceLocal : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly Globals _globals;
        private readonly CleanupOptions _cleanupOptions;

        public CleanupWorkspaceLocal(TextWriter stdout, Globals globals, CleanupOptions cleanupOptions)
        {
            _stdout = stdout;
            _globals = globals;
            _cleanupOptions = cleanupOptions;
        }

        public OptionSet OptionSet
        {
            get { return _cleanupOptions.OptionSet; }
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
            _stdout.WriteLine("Cleaning up workspaces directory for TFS remote " + remote.Id);
            remote.CleanupWorkspaceDirectory();
        }
    }
}
