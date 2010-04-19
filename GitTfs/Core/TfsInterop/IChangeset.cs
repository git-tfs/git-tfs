using System;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IChangeset
    {
        IEnumerable<IChange> Changes { get; }
        string Committer { get; }
        DateTime CreationDate { get; }
        string Comment { get; }
        int ChangesetId { get; }
    }
}