using System.Collections.Generic;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IList<string> ReadAllRemotes();
        IEnumerable<GitTfsRemote> ReadAllTfsRemotes();
        GitTfsRemote ReadTfsRemote(string remoteId);
    }
}
