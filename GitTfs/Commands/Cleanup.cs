﻿using System;
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

        public Cleanup(CleanupWorkspaces cleanupWorkspaces)
        {
            _cleanupWorkspaces = cleanupWorkspaces;
        }

        public OptionSet OptionSet
        {
            get { return _cleanupWorkspaces.OptionSet; }
        }


        public int Run()
        {
            return Choose(_cleanupWorkspaces.Run());
        }

        private int Choose(params int[] results)
        {
            return results.Where(x => x != GitTfsExitCodes.OK).FirstOr(GitTfsExitCodes.OK);
        }
    }
}
