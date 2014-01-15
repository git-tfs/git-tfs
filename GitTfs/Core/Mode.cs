using System;
using LibGit2Sharp.Core;

namespace Sep.Git.Tfs.Core
{
    /// <summary>
    /// Common modes for git tree entries (files).
    /// </summary>
    public static class Mode
    {
        /// <summary>
        /// The default mode for new files. (S_IFREG | S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH)
        /// </summary>
        public static readonly string NewFile = LibGit2Sharp.Mode.NonExecutableFile.ToModeString();

        public static LibGit2Sharp.Mode ToFileMode(this string mode)
        {
            return (LibGit2Sharp.Mode)Convert.ToInt32(mode, 8);
        }

        public static string ToModeString(this LibGit2Sharp.Mode mode)
        {
            return Convert.ToString((int)mode, 8).PadLeft(6, '0');
        }
    }
}
