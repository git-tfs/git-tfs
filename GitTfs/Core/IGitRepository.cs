using System.Collections.Generic;
using System.IO;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        void /*or IGitTfsRemote*/ CreateTfsRemote(string remoteId, string tfsUrl, string tfsRepositoryPath, RemoteOptions remoteOptions);
        void /*or IGitTfsRemote*/ CreateTfsRemote(string remoteId, TfsChangesetInfo tfsHead);
        bool HasRemote(string remoteId);
        IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head);
        IEnumerable<TfsChangesetInfo> GetParentTfsCommits(string head, bool includeStubRemotes);
        IDictionary<string, GitObject> GetObjects(string commit);
        string HashAndInsertObject(string filename);
        string HashAndInsertObject(Stream data);
        string HashAndInsertObject(Stream data, long length);
        IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to);
        string GetChangeSummary(string from, string to);
        bool WorkingCopyHasUnstagedOrUncommitedChanges { get; }
        void GetBlob(string sha, string outputFile);
        GitCommit GetCommit(string commitish);
        Dictionary<string, GitObject> GetObjects();
        string GetCommitMessage(string head, string parentCommitish);
    }
}
