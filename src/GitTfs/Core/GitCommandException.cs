using System;
using System.Diagnostics;

namespace Sep.Git.Tfs.Core
{
    public class GitCommandException : Exception
    {
        public Process Process { get; set; }

        public GitCommandException(string message, Process process) : base(message)
        {
            Process = process;
        }
    }
}
