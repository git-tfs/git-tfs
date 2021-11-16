using System;
using System.Collections.Generic;
using GitTfs.Core.TfsInterop;
using GitTfs.Commands;

namespace GitTfs.Core
{
    internal class DerivedGitTfsRemote : IGitTfsRemote
    {
        private readonly string _tfsUrl;
        private readonly string _tfsRepositoryPath;

        public DerivedGitTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            _tfsUrl = tfsUrl;
            _tfsRepositoryPath = tfsRepositoryPath;
        }

        private GitTfsException DerivedRemoteException
        {
            get
            {
                return new GitTfsException("Unable to locate a remote for <" + _tfsUrl + ">" + _tfsRepositoryPath)
                    .WithRecommendation("Try using `git tfs bootstrap` to auto-init TFS remotes.")
                    .WithRecommendation("Try setting a legacy-url for an existing remote.");
            }
        }

        public bool IsDerived
        {
            get { return true; }
        }

        public string Id
        {
            get { return "(derived)"; }
        }

        public string TfsUrl
        {
            get { return _tfsUrl; }
        }

        public bool Autotag
        {
            get { throw DerivedRemoteException; }
        }

        public string TfsUsername
        {
            get
            {
                throw DerivedRemoteException;
            }
            set
            {
                throw DerivedRemoteException;
            }
        }

        public string TfsPassword
        {
            get
            {
                throw DerivedRemoteException;
            }
            set
            {
                throw DerivedRemoteException;
            }
        }

        public string TfsRepositoryPath
        {
            get { return _tfsRepositoryPath; }
        }

        public string[] TfsSubtreePaths
        {
            get
            {
                throw DerivedRemoteException;
            }
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(DerivedGitTfsRemote)) return false;
            return Equals((DerivedGitTfsRemote)obj);
        }

        private bool Equals(DerivedGitTfsRemote other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._tfsUrl, _tfsUrl) && Equals(other._tfsRepositoryPath, _tfsRepositoryPath);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_tfsUrl != null ? _tfsUrl.GetHashCode() : 0) * 397) ^ (_tfsRepositoryPath != null ? _tfsRepositoryPath.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DerivedGitTfsRemote left, DerivedGitTfsRemote right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DerivedGitTfsRemote left, DerivedGitTfsRemote right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region All this is not implemented

        public string IgnoreRegexExpression
        {
            get { throw DerivedRemoteException; }
        }

        public string IgnoreExceptRegexExpression
        {
            get { throw DerivedRemoteException; }
        }

        public IGitRepository Repository
        {
            get { throw DerivedRemoteException; }
        }

        public ITfsHelper Tfs
        {
            get { throw DerivedRemoteException; }
            set { throw DerivedRemoteException; }
        }

        public int MaxChangesetId
        {
            get { throw DerivedRemoteException; }
            set { throw DerivedRemoteException; }
        }

        public string MaxCommitHash
        {
            get { throw DerivedRemoteException; }
            set { throw DerivedRemoteException; }
        }

        public string RemoteRef
        {
            get { throw DerivedRemoteException; }
        }

        public bool IsSubtree
        {
            get { return false; }
        }

        public bool IsSubtreeOwner
        {
            get { return false; }
        }

        public string OwningRemoteId
        {
            get { return null; }
        }

        public string Prefix
        {
            get { return null; }
        }

        public bool ExportMetadatas { get; set; }
        public Dictionary<string, IExportWorkItem> ExportWorkitemsMapping { get; set; }

        public int? GetFirstChangeset()
        {
            throw DerivedRemoteException;
        }

        public void SetFirstChangeset(int? changesetId)
        {
            throw DerivedRemoteException;
        }

        public bool ShouldSkip(string path)
        {
            throw DerivedRemoteException;
        }

        public bool IsIgnored(string path)
        {
            throw DerivedRemoteException;
        }

        public bool IsInDotGit(string path)
        {
            throw DerivedRemoteException;
        }

        public IGitTfsRemote InitBranch(RemoteOptions remoteOptions, string tfsRepositoryPath, int shaRootChangesetId, bool fetchParentBranch, string gitBranchNameExpected = null, IRenameResult renameResult = null)
        {
            throw new NotImplementedException();
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            throw DerivedRemoteException;
        }

        public IFetchResult Fetch(bool stopOnFailMergeCommit = false, int lastChangesetIdToFetch = -1, IRenameResult renameResult = null)
        {
            throw DerivedRemoteException;
        }

        public IFetchResult FetchWithMerge(int mergeChangesetId, bool stopOnFailMergeCommit = false, IRenameResult renameResult = null, params string[] parentCommitsHashes)
        {
            throw DerivedRemoteException;
        }

        public void QuickFetch(int changesetId, bool ignoreRestricted, bool printRestrictionHint)
        {
            throw DerivedRemoteException;
        }

        public void Unshelve(string a, string b, string c, Action<Exception> h, bool force)
        {
            throw DerivedRemoteException;
        }

        public void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, CheckinOptions options, bool evaluateCheckinPolicies)
        {
            throw DerivedRemoteException;
        }

        public bool HasShelveset(string shelvesetName)
        {
            throw DerivedRemoteException;
        }

        public int CheckinTool(string head, TfsChangesetInfo parentChangeset)
        {
            throw DerivedRemoteException;
        }

        public int Checkin(string treeish, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null)
        {
            throw DerivedRemoteException;
        }

        public int Checkin(string head, string parent, TfsChangesetInfo parentChangeset, CheckinOptions options, string sourceTfsPath = null)
        {
            throw DerivedRemoteException;
        }

        public void CleanupWorkspace()
        {
            throw DerivedRemoteException;
        }

        public void CleanupWorkspaceDirectory()
        {
            throw DerivedRemoteException;
        }

        public ITfsChangeset GetChangeset(int changesetId)
        {
            throw DerivedRemoteException;
        }

        public void UpdateTfsHead(string commitHash, int changesetId)
        {
            throw DerivedRemoteException;
        }

        public void EnsureTfsAuthenticated()
        {
            throw DerivedRemoteException;
        }

        public bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath)
        {
            throw DerivedRemoteException;
        }

        public void DeleteShelveset(string shelvesetName)
        {
            throw DerivedRemoteException;
        }

        public RemoteInfo RemoteInfo
        {
            get { throw DerivedRemoteException; }
        }

        public void Merge(string sourceTfsPath, string targetTfsPath)
        {
            throw DerivedRemoteException;
        }
        #endregion
    }
}
