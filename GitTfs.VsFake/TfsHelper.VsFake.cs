using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;

namespace Sep.Git.Tfs.VsFake
{
    public class TfsHelper : ITfsHelper
    {
        #region misc/null

        IContainer _container;

        public TfsHelper(IContainer container)
        {
            _container = container;
        }

        public string TfsClientLibraryVersion { get { return "(FAKE)"; } }

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string[] LegacyUrls { get; set; }

        public void EnsureAuthenticated() {}

        public bool CanShowCheckinDialog { get { return false; } }

        public long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string username)
        {
            return new NullIdentity();
        }

        #endregion

        #region read changesets

        public ITfsChangeset GetLatestChangeset(GitTfsRemote remote)
        {
            return TfsPlugin.Script.Changesets.LastOrDefault().AndAnd(x => BuildTfsChangeset(x, remote));
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, GitTfsRemote remote)
        {
            return TfsPlugin.Script.Changesets.Where(x => x.Id > startVersion).Select(x => BuildTfsChangeset(x, remote));
        }

        private ITfsChangeset BuildTfsChangeset(ScriptedChangeset changeset, GitTfsRemote remote)
        {
            var tfsChangeset = _container.With<ITfsHelper>(this).With<IChangeset>(new Changeset(changeset)).GetInstance<TfsChangeset>();
            tfsChangeset.Summary = new TfsChangesetInfo { ChangesetId = changeset.Id, Remote = remote };
            return tfsChangeset;
        }

        class Changeset : IChangeset
        {
            private ScriptedChangeset _changeset;

            public Changeset(ScriptedChangeset changeset)
            {
                _changeset = changeset;
            }

            public IChange[] Changes
            {
                get { return _changeset.Changes.Select(x => new Change(_changeset, x)).ToArray(); }
            }

            public string Committer
            {
                get { return "todo"; }
            }

            public DateTime CreationDate
            {
                get { return _changeset.CheckinDate; }
            }

            public string Comment
            {
                get { return _changeset.Comment; }
            }

            public int ChangesetId
            {
                get { return _changeset.Id; }
            }

            public IVersionControlServer VersionControlServer
            {
                get { throw new NotImplementedException(); }
            }
        }

        class Change : IChange, IItem
        {
            ScriptedChangeset _changeset;
            ScriptedChange _change;

            public Change(ScriptedChangeset changeset, ScriptedChange change)
            {
                _changeset = changeset;
                _change = change;
            }

            TfsChangeType IChange.ChangeType
            {
                get { return _change.ChangeType; }
            }

            IItem IChange.Item
            {
                get { return this; }
            }

            IVersionControlServer IItem.VersionControlServer
            {
                get { throw new NotImplementedException(); }
            }

            int IItem.ChangesetId
            {
                get { return _changeset.Id; }
            }

            string IItem.ServerItem
            {
                get { return _change.RepositoryPath; }
            }

            decimal IItem.DeletionId
            {
                get { return 0; }
            }

            TfsItemType IItem.ItemType
            {
                get { return _change.ItemType; }
            }

            int IItem.ItemId
            {
                get { throw new NotImplementedException(); }
            }

            long IItem.ContentLength
            {
                get
                {
                    using (var temp = ((IItem)this).DownloadFile())
                        return new FileInfo(temp).Length;
                }
            }

            TemporaryFile IItem.DownloadFile()
            {
                var temp = new TemporaryFile();
                using(var writer = new StreamWriter(temp))
                    writer.Write(_change.Content);
                return temp;
            }
        }

        #endregion

        #region unimplemented

        public void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            throw new NotImplementedException();
        }

        public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetChangeset(int changesetId, GitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public IChangeset GetChangeset(int changesetId)
        {
            throw new NotImplementedException();
        }

        public bool MatchesUrl(string tfsUrl)
        {
            throw new NotImplementedException();
        }

        public bool HasShelveset(string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public void CleanupWorkspaces(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
