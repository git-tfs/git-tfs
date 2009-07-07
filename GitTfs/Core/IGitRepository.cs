using System;
using System.Collections.Generic;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IEnumerable<GitTfsRemote> ReadAllTfsRemotes();
        GitTfsRemote ReadTfsRemote(string remoteId);
        string HashAndInsertObject(string filename);
    }
}
