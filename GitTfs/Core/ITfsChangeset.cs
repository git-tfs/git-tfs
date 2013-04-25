using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public interface ITfsChangeset
    {
        TfsChangesetInfo Summary { get; }
        LogEntry Apply(string lastCommit, GitIndexInfo index, ITfsWorkspace workspace);
        LogEntry CopyTree(GitIndexInfo index, ITfsWorkspace workspace);

        IChange[] Changes { get; }

        /// <summary>
        /// Get all items (files and folders) in the source TFS repository.
        /// </summary>
        IEnumerable<TfsTreeEntry> GetFullTree();

        /// <summary>
        /// Get all files that git-tfs should copy from the source TFS repository. (skips folders and ignored files)
        /// </summary>
        IEnumerable<TfsTreeEntry> GetTree();
    }
}
