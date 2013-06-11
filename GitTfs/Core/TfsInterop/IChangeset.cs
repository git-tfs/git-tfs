using System;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IChangeset : IDisposable
    {
        IChange [] Changes { get; }
        string Committer { get; }
        DateTime CreationDate { get; }
        string Comment { get; }
        int ChangesetId { get; }
        IVersionControlServer VersionControlServer { get; }
        void Get(IWorkspace workspace);
    }
}