using System.Collections.Generic;
using System.IO;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        IGitTfsRemote ReadTfsRemote(string tfsUrl, string tfsRepositoryPath);
        void /*or IGitTfsRemote*/ CreateTfsRemote(string remoteId, string tfsUrl, string tfsRepositoryPath, RemoteOptions remoteOptions);
        bool HasRemote(string remoteId);
        IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head, ICollection<string> localCommits);
        IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head);
        IDictionary<string, GitObject> GetObjects(string commit);
        string HashAndInsertObject(string filename);
        string HashAndInsertObject(Stream data);
        IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to);
        string GetChangeSummary(string from, string to);
        void GetBlob(string sha, string outputFile);
        GitCommit GetCommit(string commitish);
    }
}
