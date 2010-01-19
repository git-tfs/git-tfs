using System;

namespace Sep.Git.Tfs.Core
{
    /// <summary>
    /// Common modes for git tree entries (files).
    /// </summary>
    static class Mode
    {
        /// <summary>
        /// The default mode for new files. (S_IFREG | S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH)
        /// </summary>
        public const string NewFile = "100644";

        /// <summary>
        /// The mode for submodules. (S_IFGITLINK =~ S_IFDIR | S_IFLNK)
        /// </summary>
        public const string GitLink = "160000";

        /// <summary>
        /// bit mask for the file type bit fields. (S_IFMT)
        /// </summary>
        public const int FileTypeBitFields = 0170000;

        public static bool IsGitLink(string mode)
        {
            return (mode.ToMode() & FileTypeBitFields) == GitLink.ToMode();
        }

        public static int ToMode(this string mode)
        {
            return Convert.ToInt32(mode, 8);
        }
    }
}
