using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using Sep.Git.Tfs.VsCommon;
using StructureMap;
using System.Diagnostics;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Vs2008
{
    public class TfsHelper : TfsHelperBase
    {
        private TeamFoundationServer _server;

        public TfsHelper(TextWriter stdout, TfsApiBridge bridge, IContainer container) : base(stdout, bridge, container)
        {
        }

        public override string TfsClientLibraryVersion
        {
            get { return "" + typeof (TeamFoundationServer).Assembly.GetName().Version + " (MS)"; }
        }

        public override void EnsureAuthenticated()
        {
            if (string.IsNullOrEmpty(Url))
            {
                _server = null;
            }
            else
            {
                _server = HasCredentials ?
                    new TeamFoundationServer(Url, GetCredential(), new UICredentialsProvider()) :
                    new TeamFoundationServer(Url, new UICredentialsProvider());

                _server.EnsureAuthenticated();
            }
        }

        protected override T GetService<T>()
        {
            return (T) _server.GetService(typeof (T));
        }

        protected override string GetAuthenticatedUser()
        {
            return VersionControl.AuthenticatedUser;
        }

        public override bool CanShowCheckinDialog
        {
            get { return false; }
        }

        public override long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        public override int GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            Trace.WriteLine("TFS 2008 Compatible mode!");
            int firstChangesetIdOfParentBranch = 1;

            if (string.IsNullOrWhiteSpace(tfsPathParentBranch))
                throw new GitTfsException("This version of TFS Server doesn't permit to use this command :(\nTry using option '--parent-branch'...");

            var changesetIdsFirstChangesetInMainBranch = VersionControl.GetMergeCandidates(tfsPathParentBranch, tfsPathBranchToCreate, RecursionType.Full).Select(c => c.Changeset.ChangesetId).FirstOrDefault();

            if (changesetIdsFirstChangesetInMainBranch == 0)
            {
                Trace.WriteLine("No changeset in main branch since branch done... (need only to find the last changeset in the main branch)");
                return VersionControl.QueryHistory(tfsPathParentBranch, VersionSpec.Latest, 0,
                        RecursionType.Full, null, new ChangesetVersionSpec(firstChangesetIdOfParentBranch), VersionSpec.Latest,
                        1, false, false).Cast<Changeset>().First().ChangesetId;
            }

            Trace.WriteLine("First changeset in the main branch after branching : " + changesetIdsFirstChangesetInMainBranch);

            Trace.WriteLine("Try to find the previous changeset...");
            int step = 100;
            int upperBound = changesetIdsFirstChangesetInMainBranch - 1;
            int lowerBound = Math.Max(upperBound - step, 1);
            //for optimization, retrieve the lesser possible changesets... so 100 by 100
            while (true)
            {
                Trace.WriteLine("Looking for the changeset between changeset id " + lowerBound + " and " + upperBound);
                var firstBranchChangesetIds = VersionControl.QueryHistory(tfsPathParentBranch, VersionSpec.Latest, 0, RecursionType.Full,
                                null, new ChangesetVersionSpec(lowerBound), new ChangesetVersionSpec(upperBound), int.MaxValue, true,
                                false, false).Cast<Changeset>().Select(c => c.ChangesetId).ToList();
                if (firstBranchChangesetIds.Count != 0)
                    return firstBranchChangesetIds.First(cId => cId < changesetIdsFirstChangesetInMainBranch);
                else
                {
                    if (upperBound == 1)
                    {
                        throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find a previous changeset to changeset n°" + changesetIdsFirstChangesetInMainBranch + " in the branch!!!");
                    }
                    upperBound = Math.Max(upperBound - step, 1);
                    lowerBound = Math.Max(upperBound - step, 1);
                }
            }
        }
    }

    public class ItemDownloadStrategy : IItemDownloadStrategy
    {
        private readonly TfsApiBridge _bridge;

        public ItemDownloadStrategy(TfsApiBridge bridge)
        {
            _bridge = bridge;
        }

        public TemporaryFile DownloadFile(IItem item)
        {
            var tempfile = new TemporaryFile();
            _bridge.Unwrap<Item>(item).DownloadFile(tempfile);
            return tempfile;
        }
    }
}
