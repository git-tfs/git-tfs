using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("cleanup")]
    [RequiresValidGitRepository]
    public class Cleanup : GitTfsCommand
    {
        private readonly CleanupWorkspaces _cleanupWorkspaces;
        private readonly CleanupWorkspaceLocal _cleanupWorkspaceLocal;

        public Cleanup(CleanupWorkspaces cleanupWorkspaces, CleanupWorkspaceLocal cleanupWorkspaceLocal)
        {
            _cleanupWorkspaces = cleanupWorkspaces;
            _cleanupWorkspaceLocal = cleanupWorkspaceLocal;
        }

        public OptionSet OptionSet
        {
            get { return _cleanupWorkspaces.OptionSet; }
        }
        
        public CancellationToken Token { get; set; }

        public int Run()
        {
            return RunAll(_cleanupWorkspaces.Run, _cleanupWorkspaceLocal.Run);
        }

        private int RunAll(params Func<int>[] cleaners)
        {
            var result = GitTfsExitCodes.OK;
            foreach (var cleaner in cleaners)
            {
                result = cleaner();
                if (result != GitTfsExitCodes.OK)
                    return result;
            }
            return GitTfsExitCodes.OK;
        }
    }
}
