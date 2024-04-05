using NDesk.Options;
using GitTfs.Core;
using StructureMap;

namespace GitTfs.Commands
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

        public OptionSet OptionSet => _cleanupWorkspaces.OptionSet;

        public int Run() => RunAll(_cleanupWorkspaces.Run, _cleanupWorkspaceLocal.Run);

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
