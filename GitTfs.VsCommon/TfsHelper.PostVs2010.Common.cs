using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.BranchVisitors;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.VsCommon
{
    public abstract class TfsHelperVs2010Base : TfsHelperBase
    {
        TfsApiBridge _bridge;

        public TfsHelperVs2010Base(TextWriter stdout, TfsApiBridge bridge, IContainer container)
            : base(stdout, bridge, container)
        {
            _bridge = bridge;
        }

        public override bool CanGetBranchInformation { get { return true; } }

        public override IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            return VersionControl.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => b.Properties.ParentBranch == null)
                .Select(b => b.Properties.RootItem.Item);
        }

        public override IEnumerable<IBranchObject> GetBranches()
        {
            var branches = VersionControl.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => b.Properties.RootItem.IsDeleted == false);
            return _bridge.Wrap<WrapperForBranchObject, BranchObject>(branches);
        }

        public override int GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            if (!string.IsNullOrWhiteSpace(tfsPathParentBranch))
                Trace.WriteLine("Parameter about parent branch will be ignored because this version of TFS is able to find the parent!");

            Trace.WriteLine("Looking for all branches...");
            var allTfsBranches = VersionControl.QueryRootBranchObjects(RecursionType.Full);
            var tfsBranchToCreate = allTfsBranches.FirstOrDefault(b => b.Properties.RootItem.Item.ToLower() == tfsPathBranchToCreate.ToLower());
            if (tfsBranchToCreate == null)
                return -1;
            tfsPathParentBranch = tfsBranchToCreate.Properties.ParentBranch.Item;
            Trace.WriteLine("Found parent branch : " + tfsPathParentBranch);

            int firstChangesetIdOfParentBranch = ((ChangesetVersionSpec)tfsBranchToCreate.Properties.ParentBranch.Version).ChangesetId;

            var firstChangesetInBranchToCreate = VersionControl.QueryHistory(tfsPathBranchToCreate, VersionSpec.Latest, 0, RecursionType.Full,
                null, null, null, int.MaxValue, true, false, false).Cast<Changeset>().LastOrDefault();

            if (firstChangesetInBranchToCreate == null)
            {
                throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find first changeset for " + tfsPathBranchToCreate);
            }

            var mergedItemsToFirstChangesetInBranchToCreate =
                VersionControl.TrackMerges(new int[] {firstChangesetInBranchToCreate.ChangesetId},
                                           new ItemIdentifier(tfsPathBranchToCreate),
                                           new ItemIdentifier[] {new ItemIdentifier(tfsPathParentBranch),}, null);

            // Find the last changeset that was created before the first one in the new branch to be created.
            var lastChangesetsMergeFromParentBranch = mergedItemsToFirstChangesetInBranchToCreate.LastOrDefault(
                    e => e.SourceChangeset.ChangesetId < firstChangesetInBranchToCreate.ChangesetId);

            if (lastChangesetsMergeFromParentBranch == null)
            {
                throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find root changeset for " + tfsPathBranchToCreate + " branch in " + tfsPathParentBranch + " branch");
            }

            var rootChangesetInParentBranch = lastChangesetsMergeFromParentBranch.SourceChangeset;

            return rootChangesetInParentBranch.ChangesetId;
        }
    }

}
