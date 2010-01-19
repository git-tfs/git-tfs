namespace Sep.Git.Tfs.Core
{
    /// <summary>
    /// Common modes for git tree entries (files).
    /// </summary>
    static class Mode
    {
        /// <summary>
        /// The default mode for new files.
        /// </summary>
        public const string NewFile = "100644";

        /// <summary>
        /// The mode for submodules.
        /// </summary>
        public const string Submodule = "160000";
    }
}
