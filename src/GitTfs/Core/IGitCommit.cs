using System;
using System.Collections.Generic;

namespace GitTfs.Core
{
    public interface IGitCommit
    {
        Tuple<string, string> AuthorAndEmail { get; }
        string Message { get; }
        IEnumerable<GitCommit> Parents { get; }
        string Sha { get; }
        DateTimeOffset When { get; }

        IEnumerable<GitTreeEntry> GetTree();
    }
}