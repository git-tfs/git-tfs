using System;
using NDesk.Options;
using Sep.Git.Tfs.Core;
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

        public int Run()
        {
            return RunAll(_cleanupWorkspaces.Run, _cleanupWorkspaceLocal.Run);
        }

        private int RunAll(params Func<int>[] cleaners)
        {
            foreach (var cleaner in cleaners)
            {
                var result = cleaner();
                if (result != GitTfsExitCodes.OK)
                    return result;
            }
            return GitTfsExitCodes.OK;
        }
    }
}
