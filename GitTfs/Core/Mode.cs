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
            switch (mode)
            {
                case "100644": return LibGit2Sharp.Mode.NonExecutableFile;
                case "040000": return LibGit2Sharp.Mode.Directory;
                case "160000": return LibGit2Sharp.Mode.GitLink;
                case "120000": return LibGit2Sharp.Mode.SymbolicLink;
                default: throw new ArgumentException();
            }
        }

        public static string ToModeString(this LibGit2Sharp.Mode mode)
        {
            switch (mode)
            {
                case LibGit2Sharp.Mode.NonExecutableFile: return "100644"; 
                case LibGit2Sharp.Mode.Directory: return "040000";
                case LibGit2Sharp.Mode.GitLink: return "160000"; 
                case LibGit2Sharp.Mode.SymbolicLink: return "120000";
                default: throw new ArgumentException();
            }
        }
    }
}
