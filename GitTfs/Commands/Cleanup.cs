using System;
using System.Collections.Generic;
using System.Linq;
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


        public int Run()
        {
            var result = Choose(_cleanupWorkspaces.Run());
            if (result != GitTfsExitCodes.OK)
                return result;
            return Choose(_cleanupWorkspaceLocal.Run());
        }

        private int Choose(params int[] results)
        {
            return results.Where(x => x != GitTfsExitCodes.OK).FirstOr(GitTfsExitCodes.OK);
        }
    }
}
