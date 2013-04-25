using System;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Core
{
    class DerivedGitTfsRemote : IGitTfsRemote
    {
        private readonly string _tfsUrl;
        private readonly string _tfsRepositoryPath;

        public DerivedGitTfsRemote(string tfsUrl, string tfsRepositoryPath)
        {
            _tfsUrl = tfsUrl;
            _tfsRepositoryPath = tfsRepositoryPath;
        }

        public bool IsDerived
        {
            get { return true; }
        }

        public string Id
        {
            get { return "(derived)"; }
            set { throw new NotImplementedException(); }
        }

        public string TfsUrl
        {
            get { return _tfsUrl; }
            set { throw new NotImplementedException(); }
        }

        public bool Autotag
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string TfsUsername
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string TfsPassword
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string TfsRepositoryPath
        {
            get { return _tfsRepositoryPath; }
            set { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IGitRepository Repository
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ITfsHelper Tfs
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public long MaxChangesetId
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string MaxCommitHash
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string RemoteRef
        {
            get { throw new NotImplementedException(); }
        }

        public bool ShouldSkip(string path)
        {
            throw new NotImplementedException();
        }

        public string GetPathInGitRepo(string tfsPath)
        {
            throw new NotImplementedException();
        }

        public void Fetch()
        {
            throw new NotImplementedException();
        }

        public void FetchWithMerge(long mergeChangesetId, params string[] parentCommitsHashes)
        {
            throw new NotImplementedException();
        }

        public void QuickFetch()
        {
            throw new NotImplementedException();
        }

        public void QuickFetch(int changesetId)
        {
            throw new NotImplementedException();
        }

        public void Unshelve(string a, string b, string c)
        {
            throw new NotImplementedException();
        }

        public void Shelve(string shelvesetName, string treeish, TfsChangesetInfo parentChangeset, bool evaluateCheckinPolicies)
        {
            throw new NotImplementedException();
        }

        public bool HasShelveset(string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public long CheckinTool(string head, TfsChangesetInfo parentChangeset)
        {
            throw new NotImplementedException();
        }

        public long Checkin(string treeish, TfsChangesetInfo parentChangeset, CheckinOptions options)
        {
            throw new NotImplementedException();
        }

        public long Checkin(string head, string parent, TfsChangesetInfo parentChangeset, CheckinOptions options)
        {
            throw new NotImplementedException();
        }

        public void CleanupWorkspace()
        {
            throw new NotImplementedException();
        }

        public void CleanupWorkspaceDirectory()
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetChangeset(long changesetId)
        {
            throw new NotImplementedException();
        }

        public void UpdateTfsHead(string commitHash, long changesetId)
        {
            throw new NotImplementedException();
        }

        public void EnsureTfsAuthenticated()
        {
            throw new NotImplementedException();
        }

        public bool MatchesUrlAndRepositoryPath(string tfsUrl, string tfsRepositoryPath)
        {
            throw new NotImplementedException();
        }

        #endregion


        public RemoteInfo RemoteInfo
        {
            get { throw new NotImplementedException(); }
        }
    }
}
