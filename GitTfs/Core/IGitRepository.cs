using System.Collections.Generic;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IEnumerable<GitTfsRemote> ReadAllTfsRemotes();
        GitTfsRemote ReadTfsRemote(string remoteId);
    }
}
