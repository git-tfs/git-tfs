namespace GitTfs.Core
{
    public interface ITfsChangeset
    {
        TfsChangesetInfo Summary { get; }
        int BaseChangesetId { get; }
        LogEntry Apply(string lastCommit, IGitTreeModifier treeBuilder, ITfsWorkspace workspace, IDictionary<string, GitObject> initialTree, Action<Exception> ignorableErrorHandler);
        LogEntry CopyTree(IGitTreeModifier treeBuilder, ITfsWorkspace workspace);

        /// <summary>
        /// Get all items (files and folders) in the source TFS repository.
        /// </summary>
        IEnumerable<TfsTreeEntry> GetFullTree();

        /// <summary>
        /// Get all files that git-tfs should copy from the source TFS repository. (skips folders and ignored files)
        /// </summary>
        IEnumerable<TfsTreeEntry> GetTree();

        /// <summary>
        /// Get if this changeset is a merge changeset
        /// </summary>
        bool IsMergeChangeset { get; }

        /// <summary>
        /// Get parent that not was fetched
        /// </summary>
        string OmittedParentBranch { get; set; }

        bool IsBranchRenameChangeset { get; }
    }
}
