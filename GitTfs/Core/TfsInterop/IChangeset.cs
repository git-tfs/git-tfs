using System;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IChangeset
    {
        IChange[] Changes { get; }
        string Committer { get; }
        DateTime CreationDate { get; }
        string Comment { get; }
        int ChangesetId { get; }
        IVersionControlServer VersionControlServer { get; }
        void Get(ITfsWorkspace workspace, IEnumerable<IChange> changes, Action<Exception> ignorableErrorHandler);
    }
}