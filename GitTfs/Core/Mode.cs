using System;
using GitSharp.Core;

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
        public static readonly string NewFile = FileMode.RegularFile.ToModeString();// "100644";

        public static FileMode ToFileMode(this string mode)
        {
            return FileMode.FromBits(Convert.ToInt32(mode, 8));
        }

        public static string ToModeString(this FileMode mode)
        {
            var modeString = Convert.ToString(mode.Bits, 8);
            while(modeString.Length < 6)
            {
                modeString = "0" + modeString;
            }
            return modeString;
        }
    }
}
