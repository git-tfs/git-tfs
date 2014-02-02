using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Client;
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
        protected TfsTeamProjectCollection _server;

        public TfsHelperVs2010Base(TextWriter stdout, TfsApiBridge bridge, IContainer container)
            : base(stdout, bridge, container)
        {
            _bridge = bridge;
        }

        public override bool CanGetBranchInformation
        {
            get
            {
                var is2008OrOlder = (_server.ConfigurationServer == null);
                return !is2008OrOlder;
            }
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

        public override int GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            try
            {
                if (!CanGetBranchInformation)
                {
                    Trace.WriteLine("Try TFS2008 compatibility mode...");
                    return base.GetRootChangesetForBranch(tfsPathBranchToCreate, tfsPathParentBranch);
                }

                if (!string.IsNullOrWhiteSpace(tfsPathParentBranch))
                    Trace.WriteLine("Parameter about parent branch will be ignored because this version of TFS is able to find the parent!");

                Trace.WriteLine("Looking for all branches...");
                var allTfsBranches = VersionControl.QueryRootBranchObjects(RecursionType.Full);
                var tfsBranchToCreate = allTfsBranches.FirstOrDefault(b => b.Properties.RootItem.Item.ToLower() == tfsPathBranchToCreate.ToLower());
                if (tfsBranchToCreate == null)
                {
                    throw new GitTfsException("error: TFS branches "+ tfsPathBranchToCreate +" not found!");
                }

                if (tfsBranchToCreate.Properties.ParentBranch == null)
                {
                    throw new GitTfsException("error : the branch you try to init '" + tfsPathBranchToCreate + "' is a root branch (e.g. has no parents).",
                        new List<string> { "Clone this branch from Tfs instead of trying to init it!\n   Command: git tfs clone " + Url + " " + tfsPathBranchToCreate });
                }
                
                tfsPathParentBranch = tfsBranchToCreate.Properties.ParentBranch.Item;
                Trace.WriteLine("Found parent branch : " + tfsPathParentBranch);

                var firstChangesetInBranchToCreate = VersionControl.QueryHistory(tfsPathBranchToCreate, VersionSpec.Latest, 0, RecursionType.Full,
                    null, null, null, int.MaxValue, true, false, false).Cast<Changeset>().LastOrDefault();

                if (firstChangesetInBranchToCreate == null)
                {
                    throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find first changeset for " + tfsPathBranchToCreate);
                }

                var mergedItemsToFirstChangesetInBranchToCreate = VersionControl
                    .TrackMerges(new int[] {firstChangesetInBranchToCreate.ChangesetId},
                                 new ItemIdentifier(tfsPathBranchToCreate),
                                 new ItemIdentifier[] {new ItemIdentifier(tfsPathParentBranch),},
                                 null)
                    .OrderBy(x => x.SourceChangeset.ChangesetId);

                var rootChangesetInParentBranch =
                    GetRelevantChangesetBasedOnChangeType(mergedItemsToFirstChangesetInBranchToCreate, tfsPathParentBranch, tfsPathBranchToCreate);

                return rootChangesetInParentBranch.ChangesetId;
            }
            catch (FeatureNotSupportedException ex)
            {
                Trace.WriteLine(ex.Message);
                return base.GetRootChangesetForBranch(tfsPathBranchToCreate, tfsPathParentBranch);
            }
        }

        /// <summary>
        /// Gets the relevant TFS <see cref="ChangesetSummary"/> for the root changeset given a set 
        /// of <see cref="ExtendedMerge"/> objects and a given <paramref name="tfsPathParentBranch"/>.
        /// </summary>
        /// <param name="merges">An array of <see cref="ExtendedMerge"/> objects describing the a set of merges.</param>
        /// <param name="tfsPathParentBranch">The tfs Path Parent Branch.</param>
        /// <param name="tfsPathBranchToCreate">The tfs Path Branch To Create.</param>
        /// <remarks>
        /// Each <see cref="ChangeType"/> uses the SourceChangeset, SourceItem, TargetChangeset, and TargetItem 
        /// properties with different semantics, depending on what it needs to describe, so the strategy to determine
        /// whether we are interested in a given ExtendedMerge summary depends on the SourceItem's <see cref="ChangeType"/>.
        /// </remarks>
        /// <returns><value>True</value> if the given <paramref name="merge"/> is relevant; <value>False</value> otherwise.
        /// </returns>
        private static ChangesetSummary GetRelevantChangesetBasedOnChangeType(IEnumerable<ExtendedMerge> merges, string tfsPathParentBranch, string tfsPathBranchToCreate)
        {
            merges = (merges ?? new ExtendedMerge[] {}).ToArray();
            var merge = merges.LastOrDefault(m => m.SourceItem.Item.ServerItem.Equals(tfsPathParentBranch, StringComparison.InvariantCultureIgnoreCase))
                     ?? merges.LastOrDefault();

            if (merge == null)
            {
                throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find root changeset for " + tfsPathBranchToCreate + " branch in " + tfsPathParentBranch + " branch");
            }

            if (merge.SourceItem.ChangeType.HasFlag(ChangeType.Branch)
                || merge.SourceItem.ChangeType.HasFlag(ChangeType.Merge)
                || merge.SourceItem.ChangeType.HasFlag(ChangeType.Add)
                || merge.SourceItem.ChangeType.HasFlag(ChangeType.Rollback))
            {
                Trace.WriteLine("Found C" + merge.SourceChangeset.ChangesetId + " on branch " + merge.SourceItem.Item.ServerItem);
                return merge.SourceChangeset;
            }
            if (merge.SourceItem.ChangeType.HasFlag(ChangeType.Rename)
                || merge.SourceItem.ChangeType.HasFlag(ChangeType.SourceRename))
            {
                Trace.WriteLine("Found C" + merge.TargetChangeset.ChangesetId + " on branch " + merge.TargetItem.Item);
                return merge.TargetChangeset;
            }
            throw new GitTfsException(
                "Don't know (yet) how to find the root changeset for an ExtendedMerge of type " +
                merge.SourceItem.ChangeType,
                new string[]
                            {
                                "Open an Issue on Github to notify the community that you need support for '" +
                                merge.SourceItem.ChangeType + "': https://github.com/git-tfs/git-tfs/issues"
                            });
        }

        public override void CreateBranch(string sourcePath, string targetPath, int changesetId, string comment = null)
        {
            var changesetToBranch = new ChangesetVersionSpec(changesetId);
            int branchChangesetId = VersionControl.CreateBranch(sourcePath, targetPath, changesetToBranch);

            if (comment != null)
            {
                Changeset changeset = VersionControl.GetChangeset(branchChangesetId);
                changeset.Comment = comment;
                changeset.Update();
            }
        }

        protected override void ConvertFolderIntoBranch(string tfsRepositoryPath)
        {
            VersionControl.CreateBranchObject(new BranchProperties(new ItemIdentifier(tfsRepositoryPath)));
        }
    }

}
