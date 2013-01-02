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
        public TfsHelperVs2010Base(TextWriter stdout, TfsApiBridge bridge, IContainer container)
            : base(stdout, bridge, container)
        {
        }

        public override bool CanGetBranchInformation { get { return true; } }

        public override IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            return VersionControl.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => b.Properties.ParentBranch == null)
                .Select(b => b.Properties.RootItem.Item);
        }

        public override IBranch GetRootTfsBranchForRemotePath(string remoteTfsPath, bool searchExactPath = true)
        {
            var recursionType = RecursionType.Full;
            var branches = VersionControl.QueryRootBranchObjects(recursionType)
                .Where(b => b.Properties.RootItem.IsDeleted == false)
                .ToList();

            var roots = branches.Where(b => b.Properties.ParentBranch == null).ToList();
            var children = branches.Except(roots).ToList();

            var wrapped = roots.Select(b => WrapperForBranchFactory.Wrap(b, children)).ToList();

            return wrapped.FirstOrDefault(b =>
                {
                    var visitor = new BranchContainsPathVisitor(remoteTfsPath, searchExactPath);
                    b.AcceptVisitor(visitor);
                    return visitor.Found;
                });
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

            var lastChangesetsMergeFromParentBranch = mergedItemsToFirstChangesetInBranchToCreate.LastOrDefault();

            if (lastChangesetsMergeFromParentBranch == null)
            {
                throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find root changeset for " + tfsPathBranchToCreate + " branch in " + tfsPathParentBranch + " branch");
            }

            var rootChangesetInParentBranch = lastChangesetsMergeFromParentBranch.SourceChangeset;

            return rootChangesetInParentBranch.ChangesetId;
        }
    }

}
