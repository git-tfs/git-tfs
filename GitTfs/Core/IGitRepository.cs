using System;
using System.Collections.Generic;
using LibGit2Sharp;
using Branch = LibGit2Sharp.Branch;

namespace Sep.Git.Tfs.Core
{
    public interface IGitRepository : IGitHelpers
    {
        string GitDir { get; set; }
        string GetConfig(string key);
        T GetConfig<T>(string key);
        T GetConfig<T>(string key, T defaultValue);
        void SetConfig(string key, string value);
        IEnumerable<IGitTfsRemote> ReadAllTfsRemotes();
        IGitTfsRemote ReadTfsRemote(string remoteId);
        IGitTfsRemote CreateTfsRemote(RemoteInfo remoteInfo, string autocrlf = null, string ignorecase = null);
        void DeleteTfsRemote(IGitTfsRemote remoteId);
        IEnumerable<string> GetGitRemoteBranches(string gitRemote);
        bool HasRemote(string remoteId);
        bool HasRef(string gitRef);
        GitCommit Commit(LogEntry logEntry);
        void UpdateRef(string gitRefName, string commitSha, string message = null);
        void MoveTfsRefForwardIfNeeded(IGitTfsRemote remote);
        IEnumerable<TfsChangesetInfo> GetLastParentTfsCommits(string head);
        TfsChangesetInfo GetTfsChangesetById(string remoteRef, long changesetId, string tfsPath);
        TfsChangesetInfo GetTfsCommit(GitCommit commit);
        TfsChangesetInfo GetTfsCommit(string sha);
        TfsChangesetInfo GetCurrentTfsCommit();
        IDictionary<string, GitObject> CreateObjectsDictionary();
        IDictionary<string, GitObject> GetObjects(string commit);
        IDictionary<string, GitObject> GetObjects(string commit, IDictionary<string, GitObject> initialTree);
        IGitTreeBuilder GetTreeBuilder(string commit);
        IEnumerable<IGitChangedFile> GetChangedFiles(string from, string to);
        bool WorkingCopyHasUnstagedOrUncommitedChanges { get; }
        void CopyBlob(string sha, string outputFile);
        GitCommit GetCommit(string commitish);
        MergeResult Merge(string commitish);
        string GetCurrentCommit();
        string GetCommitMessage(string head, string parentCommitish);
        string AssertValidBranchName(string gitBranchName);
        bool CreateBranch(string gitBranchName, string target);
        Branch RenameBranch(string oldName, string newName);
        string FindCommitHashByChangesetId(long changesetId, string tfsPath);
        void CreateTag(string name, string sha, string comment, string Owner, string emailOwner, System.DateTime creationDate);
        void CreateNote(string sha, string content, string owner, string emailOwner, DateTime creationDate);
        void MoveRemote(string oldRemoteName, string newRemoteName);
        void ResetHard(string sha);
        bool IsBare { get; }
        /// Gets all configured "subtree" remotes which point to the same Tfs URL as the given remote.
        /// If the given remote is itself a subtree, an empty enumerable is returned.
        /// </summary>
        IEnumerable<IGitTfsRemote> GetSubtrees(IGitTfsRemote owner);
        void ResetRemote(IGitTfsRemote remoteToReset, string target);
        string GetCurrentBranch();
        void GarbageCollect(bool auto = false, string additionalMessage = null);
        bool Checkout(string commitish);
        IEnumerable<GitCommit> FindParentCommits(string fromCommit, string toCommit);
    }
}
