using System.Collections.Generic;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath);
        IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head, ICollection<string> localCommits);
        IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head);
        IDictionary<string, GitObject> GetObjects(string commit);
        string HashAndInsertObject(string filename);
        string HashAndInsertObject(Stream data);
    }
}
