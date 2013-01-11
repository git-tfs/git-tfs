using System;
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
        private TfsApiBridge _bridge;

        public TfsHelperVs2010Base(TextWriter stdout, TfsApiBridge bridge, IContainer container)
            : base(stdout, bridge, container)
        {
            _bridge = bridge;
        }

        public override bool CanGetBranchInformation
        {
            get { return true; }
        }

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

        // TODO: pass back richer information so that the calling client (who likely has access to stdout) can surface details about the branch origin to the console.
        public override int GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            if (!string.IsNullOrWhiteSpace(tfsPathParentBranch))
                Trace.WriteLine(
                    "Parameter about parent branch will be ignored because this version of TFS is able to find the parent!");

            Trace.WriteLine("Looking for all branches...");
            var allTfsBranches = VersionControl.QueryRootBranchObjects(RecursionType.Full);
            var tfsBranchToCreate =
                allTfsBranches.FirstOrDefault(
                    b => b.Properties.RootItem.Item.ToLower() == tfsPathBranchToCreate.ToLower());
            if (tfsBranchToCreate == null)
                return -1;
            tfsPathParentBranch = tfsBranchToCreate.Properties.ParentBranch.Item;
            Trace.WriteLine("Found parent branch : " + tfsPathParentBranch);

            int firstChangesetIdOfParentBranch =
                ((ChangesetVersionSpec) tfsBranchToCreate.Properties.ParentBranch.Version).ChangesetId;

            var firstChangesetInBranchToCreate =
                VersionControl.QueryHistory(tfsPathBranchToCreate, VersionSpec.Latest, 0, RecursionType.Full,
                                            null, null, null, int.MaxValue, true, false, false)
                              .Cast<Changeset>()
                              .LastOrDefault();

            if (firstChangesetInBranchToCreate == null)
            {
                throw new GitTfsException(
                    "An unexpected error occured when trying to find the root changeset.\nFailed to find first changeset for " +
                    tfsPathBranchToCreate);
            }

            var mergedItemsToFirstChangesetInBranchToCreate =
                VersionControl.TrackMerges(new int[] {firstChangesetInBranchToCreate.ChangesetId},
                                           new ItemIdentifier(tfsPathBranchToCreate),
                                           new ItemIdentifier[] {new ItemIdentifier(tfsPathParentBranch),}, null);

            var rootChangesetInParentBranch =
                GetRelevantChangesetBasedOnChangeType(mergedItemsToFirstChangesetInBranchToCreate, tfsPathParentBranch);

            if (rootChangesetInParentBranch == null)
            {
                throw new GitTfsException(
                    "An unexpected error occured when trying to find the root changeset.\nFailed to find root changeset for " +
                    tfsPathBranchToCreate + " branch in " + tfsPathParentBranch + " branch");
            }

            return rootChangesetInParentBranch.ChangesetId;
        }

        private static ChangesetSummary GetRelevantChangesetBasedOnChangeType(ExtendedMerge[] merges, string tfsPathParentBranch)
        {
            if (merges == null) return null;

            var merge = merges
                .LastOrDefault(m => m.SourceItem.Item.ServerItem.Equals(tfsPathParentBranch, StringComparison.InvariantCultureIgnoreCase))
                ?? merges.LastOrDefault();

            if (merge == null) return null;

            switch (merge.SourceItem.ChangeType)
            {
                case ChangeType.Branch:
                    Trace.WriteLine("Found C"+merge.SourceChangeset.ChangesetId+" on branch "+merge.SourceItem.Item.ServerItem);
                    return merge.SourceChangeset;
                case ChangeType.Rename:
                    Trace.WriteLine("Found C"+merge.TargetChangeset.ChangesetId+" on branch "+merge.TargetItem.Item);
                    return merge.TargetChangeset;
                default:
                    throw new GitTfsException("Don't know (yet) how to find the root changeset for an ExtendedMerge of type " + merge.SourceItem.ChangeType,
                        new string[] { "Open an Issue on Github to notify the community that you need support for '"+merge.SourceItem.ChangeType+"': https://github.com/git-tfs/git-tfs/issues" });
            }
        }

        /// <summary>
        /// Predicate that determines if a given TFS <see cref="ExtendedMerge"/> object is relevant when searching for 
        /// the TFS ChangesetId that roots the <paramref name="targetBranch"/> to the <paramref name="parentBranch"/>.
        /// </summary>
        /// <param name="merge">An <see cref="ExtendedMerge"/> object describing the details of the merge.</param>
        /// <param name="targetBranch">The TFS repository path to the child branch that we are trying to attach.</param>
        /// <param name="parentBranch">The TFS repository path to the parent branch where we expect to find the root ChangesetId.</param>
        /// <remarks>
        /// Each <see cref="ChangeType"/> uses the SourceChangeset, SourceItem, TargetChangeset, and TargetItem 
        /// properties with different semantics, depending on what it needs to describe, so the strategy to determine
        /// whether we are interested in a given ExtendedMerge summary depends on the SourceItem's <see cref="ChangeType"/>.
        /// </remarks>
        /// <returns><value>True</value> if the given <paramref name="merge"/> is relevant; <value>False</value> otherwise.</returns>
        [Obsolete("not needed; please remove", false)]
        private static bool IsRelevantMergeSummary(ExtendedMerge merge, string targetBranch, string parentBranch)
        {
            switch (merge.SourceItem.ChangeType)
            {
                case ChangeType.Branch:
                    return merge.SourceItem.Item.ServerItem.Equals(parentBranch, StringComparison.InvariantCultureIgnoreCase);
                case ChangeType.Rename:
                    return merge.TargetItem.Item.Equals(targetBranch, StringComparison.InvariantCultureIgnoreCase);
                default:
                    return false;
            }
        }
    }

}
