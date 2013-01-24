using System.Collections.Generic;
using System.IO;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        string GitDir { get; set; }
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        void /*or IGitTfsRemote*/ CreateTfsRemote(string remoteId, string tfsUrl, string tfsRepositoryPath, RemoteOptions remoteOptions);
        void /*or IGitTfsRemote*/ CreateTfsRemote(string remoteId, TfsChangesetInfo tfsHead, RemoteOptions remoteOptions);
        bool HasRemote(string remoteId);
        bool HasRef(string gitRef);
        void MoveTfsRefForwardIfNeeded(IGitTfsRemote remote);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head, bool includeStubRemotes);
        IDictionary<string, GitObject> GetObjects(string commit);
        string HashAndInsertObject(string filename);
        IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to);
        bool WorkingCopyHasUnstagedOrUncommitedChanges { get; }
        void CopyBlob(string sha, string outputFile);
        GitCommit GetCommit(string commitish);
        Dictionary<string, GitObject> GetObjects();
        string GetCommitMessage(string head, string parentCommitish);
        string AssertValidBranchName(string gitBranchName);
        bool CreateBranch(string gitBranchName, string target);
        string FindCommitHashByCommitMessage(string patternToFind);
        void CreateTag(string name, string sha, string comment, string Owner, string emailOwner, System.DateTime creationDate);
    }
}
