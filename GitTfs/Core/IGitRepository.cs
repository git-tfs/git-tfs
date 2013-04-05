using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using Sep.Git.Tfs.Commands;
using Branch = LibGit2Sharp.Branch;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        string GitDir { get; set; }
        string GetConfig(string key);
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        IGitTfsRemote CreateTfsRemote(RemoteInfo remoteInfo);
        void DeleteTfsRemote(IGitTfsRemote remoteId);
        bool HasRemote(string remoteId);
        bool HasRef(string gitRef);
        void MoveTfsRefForwardIfNeeded(IGitTfsRemote remote);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head, bool includeStubRemotes);
        TfsChangesetInfo GetCurrentTfsCommit();
        IDictionary<string, GitObject> GetObjects(string commit);
        string HashAndInsertObject(string filename);
        IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to);
        bool WorkingCopyHasUnstagedOrUncommitedChanges { get; }
        void CopyBlob(string sha, string outputFile);
        GitCommit GetCommit(string commitish);
        Dictionary<string, GitObject> GetObjects();
        string GetCommitMessage(string head, string parentCommitish);
        string GetCommitMessage(string commitish);
        string AssertValidBranchName(string gitBranchName);
        bool CreateBranch(string gitBranchName, string target);
        Branch RenameBranch(string oldName, string newName);
        string FindCommitHashByCommitMessage(string patternToFind);
        void CreateTag(string name, string sha, string comment, string Owner, string emailOwner, System.DateTime creationDate);
        void CreateNote(string sha, string content, string owner, string emailOwner, DateTime creationDate);
        void MoveRemote(string oldRemoteName, string newRemoteName);
        void Reset(string sha, ResetOptions resetOptions);
    }
}
