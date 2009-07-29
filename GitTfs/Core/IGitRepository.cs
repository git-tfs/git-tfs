using System.Collections.Generic;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IEnumerable<GitTfsRemote> ReadAllTfsRemotes();
        GitTfsRemote ReadTfsRemote(string remoteId);
        GitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath);
        TfsChangesetInfo WorkingHeadInfo(string head, ICollection<string> localCommits);
        TfsChangesetInfo WorkingHeadInfo(string head);
        GitObject GetObjectInfo(string commit, string path);
        string HashAndInsertObject(string filename);
        string HashAndInsertObject(Stream data);
    }
}
