using System;
using System.Diagnostics;

namespace GitTfs.Core
{
    public class GitCommandException : Exception
    {
        public Process Process { get; }

        public GitCommandException(string message, Process process) : base(message)
        {
            Process = process;
        }
    }
}
