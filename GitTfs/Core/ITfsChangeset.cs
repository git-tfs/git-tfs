﻿using System.Collections.Generic;

namespace Sep.Git.Tfs.Core
{
    public interface ITfsChangeset
    {
        TfsChangesetInfo Summary { get; }
        LogEntry Apply(string lastCommit, GitIndexInfo index, ITfsWorkspace workspace);
        LogEntry CopyTree(GitIndexInfo index, ITfsWorkspace workspace);
        IEnumerable<TfsTreeEntry> GetTree();
    }
}
