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
        void SetConfig(string key, string value);
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        IGitTfsRemote CreateTfsRemote(RemoteInfo remoteInfo, string autocrlf = null, string ignorecase = null);
        void DeleteTfsRemote(IGitTfsRemote remoteId);
        bool HasRemote(string remoteId);
        bool HasRef(string gitRef);
        void UpdateRef(string gitRefName, string commitSha, string message = null);
        void MoveTfsRefForwardIfNeeded(IGitTfsRemote remote);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head, bool includeStubRemotes);
        IEnumerable<TfsChangesetInfo> FilterParentTfsCommits(string head, bool includeStubRemotes, Predicate<TfsChangesetInfo> pred);
        TfsChangesetInfo GetTfsCommit(string sha);
        TfsChangesetInfo GetCurrentTfsCommit();
        IDictionary<string, GitObject> GetObjects(string commit);
        IGitTreeBuilder GetTreeBuilder(string commit);
        IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to);
        bool WorkingCopyHasUnstagedOrUncommitedChanges { get; }
        void CopyBlob(string sha, string outputFile);
        GitCommit GetCommit(string commitish);
        string GetCurrentCommit();
        Dictionary<string, GitObject> GetObjects();
        string GetCommitMessage(string head, string parentCommitish);
        string AssertValidBranchName(string gitBranchName);
        bool CreateBranch(string gitBranchName, string target);
        Branch RenameBranch(string oldName, string newName);
        string FindCommitHashByChangesetId(long changesetId);
        void CreateTag(string name, string sha, string comment, string Owner, string emailOwner, System.DateTime creationDate);
        void CreateNote(string sha, string content, string owner, string emailOwner, DateTime creationDate);
        void MoveRemote(string oldRemoteName, string newRemoteName);
        void Reset(string sha, ResetOptions resetOptions);
        bool IsBare { get; }
        /// Gets all configured "subtree" remotes which point to the same Tfs URL as the given remote.
        /// If the given remote is itself a subtree, an empty enumerable is returned.
        /// </summary>
        IEnumerable<IGitTfsRemote> GetSubtrees(IGitTfsRemote owner);
        void ResetRemote(IGitTfsRemote remoteToReset, string target);
        string GetCurrentBranch();
    }
}
