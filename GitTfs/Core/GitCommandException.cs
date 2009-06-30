using System;

namespace Sep.Git.Tfs.Core
{
    public class GitCommandException : Exception
    {
        public string CommandLine { get; set; }
        public int ExitCode { get; set; }
    }
}
