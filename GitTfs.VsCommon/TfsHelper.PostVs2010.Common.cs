using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;
using ChangeType = Microsoft.TeamFoundation.VersionControl.Client.ChangeType;

namespace Sep.Git.Tfs.VsCommon
{
    public abstract class TfsHelperVs2010Base : TfsHelperBase
    {
        TfsApiBridge _bridge;
        protected TfsTeamProjectCollection _server;
        private static bool _resolverInstalled;

        public TfsHelperVs2010Base(TextWriter stdout, TfsApiBridge bridge, IContainer container)
            : base(stdout, bridge, container)
        {
            _bridge = bridge;
            if (!_resolverInstalled)
            {
                AppDomain.CurrentDomain.AssemblyResolve += LoadFromVsFolder;
                _resolverInstalled = true;
            }
        }

        public override bool CanGetBranchInformation
        {
            get
            {
                var is2008OrOlder = (_server.ConfigurationServer == null);
                return !is2008OrOlder;
            }
        }

        public override IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion, IGitTfsRemote remote)
        {
            const int batchCount = 100;
            var start = (int)startVersion;
            Changeset[] changesets;
            do
            {
                var startChangeset = new ChangesetVersionSpec(start);
                changesets = Retry.Do(() => VersionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
                    null, startChangeset, null, batchCount, true, true, true, true)
                    .Cast<Changeset>().ToArray());
                if (changesets.Length > 0)
                    start = changesets[changesets.Length - 1].ChangesetId + 1;

                // don't take the enumerator produced by a foreach statement or a yield statement, as there are references 
                // to the old (iterated) elements and thus the referenced changesets won't be disposed until all elements were iterated.
                for (int i = 0; i < changesets.Length; i++)
                {
                    yield return BuildTfsChangeset(changesets[i], remote);
                    changesets[i] = null;
                }
            } while (changesets.Length == batchCount);
        }

        public override IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            return VersionControl.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => b.Properties.ParentBranch == null)
                .Select(b => b.Properties.RootItem.Item);
        }

        public override IEnumerable<IBranchObject> GetBranches(bool getAlsoDeletedBranches = false)
        {
            var branches = VersionControl.QueryRootBranchObjects(RecursionType.Full);
            if (getAlsoDeletedBranches)
                return _bridge.Wrap<WrapperForBranchObject, BranchObject>(branches);
            return _bridge.Wrap<WrapperForBranchObject, BranchObject>(branches.Where(b => !b.Properties.RootItem.IsDeleted));
        }

        public override IList<RootBranch> GetRootChangesetForBranch(string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            var rootBranches = new List<RootBranch>();
            GetRootChangesetForBranch(rootBranches, tfsPathBranchToCreate, tfsPathParentBranch);
            return rootBranches;
        }

        private void GetRootChangesetForBranch(IList<RootBranch> rootBranches, string tfsPathBranchToCreate, string tfsPathParentBranch = null)
        {
            Trace.WriteLine("Looking for root changeset for branch:" + tfsPathBranchToCreate);
            try
            {
                if (!CanGetBranchInformation)
                {
                    Trace.WriteLine("Try TFS2008 compatibility mode...");
                    foreach (var rootBranch in base.GetRootChangesetForBranch(tfsPathBranchToCreate, tfsPathParentBranch))
                    {
                        AddNewRootBranch(rootBranches, rootBranch);
                    }
                    return;
                }

                if (!string.IsNullOrWhiteSpace(tfsPathParentBranch))
                    Trace.WriteLine("Parameter about parent branch will be ignored because this version of TFS is able to find the parent!");

                Trace.WriteLine("Looking to find branch '" + tfsPathBranchToCreate + "' in all TFS branches...");
                string tfsParentBranch;
                if (!AllTfsBranches.TryGetValue(tfsPathBranchToCreate, out tfsParentBranch))
                {
                    throw new GitTfsException("error: TFS branches " + tfsPathBranchToCreate + " not found!");
                }

                if (tfsParentBranch == null)
                {
                    throw new GitTfsException("error : the branch you try to init '" + tfsPathBranchToCreate + "' is a root branch (e.g. has no parents).",
                        new List<string> { "Clone this branch from Tfs instead of trying to init it!\n   Command: git tfs clone " + Url + " " + tfsPathBranchToCreate });
                }
                
                tfsPathParentBranch = tfsParentBranch;
                Trace.WriteLine("Found parent branch : " + tfsPathParentBranch);

                var firstChangesetInBranchToCreate = VersionControl.QueryHistory(tfsPathBranchToCreate, VersionSpec.Latest, 0, RecursionType.Full,
                    null, null, null, 1, false, false, false, true).Cast<Changeset>().FirstOrDefault();

                if (firstChangesetInBranchToCreate == null)
                {
                    throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find first changeset for " + tfsPathBranchToCreate);
                }

                try
                {
                    var mergedItemsToFirstChangesetInBranchToCreate = GetMergeInfo(tfsPathBranchToCreate, tfsPathParentBranch, firstChangesetInBranchToCreate.ChangesetId);

                    string renameFromBranch;
                    var rootChangesetInParentBranch =
                        GetRelevantChangesetBasedOnChangeType(mergedItemsToFirstChangesetInBranchToCreate, tfsPathParentBranch, tfsPathBranchToCreate, out renameFromBranch);

                    AddNewRootBranch(rootBranches, new RootBranch(rootChangesetInParentBranch, tfsPathBranchToCreate));
                    Trace.WriteLineIf(renameFromBranch != null, "Found original branch '" + renameFromBranch + "' (renamed in branch '" + tfsPathBranchToCreate + "')");
                    if (renameFromBranch != null)
                        GetRootChangesetForBranch(rootBranches, renameFromBranch);
                }
                catch (VersionControlException)
                {
                    throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to query history for " + tfsPathBranchToCreate);
                }
            }
            catch (FeatureNotSupportedException ex)
            {
                Trace.WriteLine(ex.Message);
                foreach (var rootBranch in base.GetRootChangesetForBranch(tfsPathBranchToCreate, tfsPathParentBranch))
                {
                    AddNewRootBranch(rootBranches, rootBranch);
                }
            }
        }

        private class MergeInfo
        {
            public ChangeType SourceChangeType;
            public int SourceChangeset;
            public string SourceItem;
            public ChangeType TargetChangeType;
            public int TargetChangeset;
            public string TargetItem;

            public override string ToString()
            {
                return string.Format("`{0}` C{1} `{2}` Source `{3}` C{4} `{5}`", TargetChangeType, TargetChangeset, TargetItem,
                    SourceChangeType, SourceChangeset, SourceItem);
            }
        }

        private IEnumerable<MergeInfo> GetMergeInfo(string tfsPathBranchToCreate, string tfsPathParentBranch,
            int firstChangesetInBranchToCreate)
        {
            var mergedItemsToFirstChangesetInBranchToCreate = new List<MergeInfo>();
            var merges = VersionControl
                .TrackMerges(new int[] {firstChangesetInBranchToCreate},
                    new ItemIdentifier(tfsPathBranchToCreate),
                    new ItemIdentifier[] {new ItemIdentifier(tfsPathParentBranch),},
                    null)
                .OrderBy(x => x.SourceChangeset.ChangesetId);
            MergeInfo lastMerge = null;
            foreach (var extendedMerge in merges.Reverse())
            {
                var sourceItem = extendedMerge.SourceItem.Item.ServerItem;
                var targetItem = extendedMerge.TargetItem != null ? extendedMerge.TargetItem.Item : null;
                var targetChangeType = extendedMerge.TargetItem != null ? extendedMerge.TargetItem.ChangeType : 0;
                if (lastMerge != null && extendedMerge.SourceItem.ChangeType == lastMerge.SourceChangeType &&
                    targetChangeType == lastMerge.TargetChangeType &&
                    sourceItem == lastMerge.SourceItem && targetItem == lastMerge.TargetItem)
                    continue;
                lastMerge = new MergeInfo
                {
                    SourceChangeType = extendedMerge.SourceItem.ChangeType,
                    SourceItem = sourceItem,
                    SourceChangeset = extendedMerge.SourceChangeset.ChangesetId,
                    TargetItem = targetItem,
                    TargetChangeset = extendedMerge.TargetChangeset.ChangesetId,
                    TargetChangeType = extendedMerge.TargetItem != null ? extendedMerge.TargetItem.ChangeType : 0
                };
                mergedItemsToFirstChangesetInBranchToCreate.Add(lastMerge);
                Trace.WriteLine(lastMerge, "Merge");
            }
            return mergedItemsToFirstChangesetInBranchToCreate;
        }

        private IDictionary<string, string> _allTfsBranches;
        private IDictionary<string, string> AllTfsBranches
        {
            get
            {
                if (_allTfsBranches != null)
                    return _allTfsBranches;
                Trace.WriteLine("Looking for all branches...");
                _allTfsBranches = VersionControl.QueryRootBranchObjects(RecursionType.Full)
                    .ToDictionary(b => b.Properties.RootItem.Item,
                        b => b.Properties.ParentBranch != null ? b.Properties.ParentBranch.Item : null,
                        (StringComparer.InvariantCultureIgnoreCase));
                return _allTfsBranches;
            }
        }

        private static void AddNewRootBranch(IList<RootBranch> rootBranches, RootBranch rootBranch)
        {
            if (rootBranches.Any())
                rootBranch.IsRenamedBranch = true;
            rootBranches.Insert(0, rootBranch);
        }

        /// <summary>
        /// Gets the relevant TFS <see cref="ChangesetSummary"/> for the root changeset given a set 
        /// of <see cref="ExtendedMerge"/> objects and a given <paramref name="tfsPathParentBranch"/>.
        /// </summary>
        /// <param name="merges">An array of <see cref="ExtendedMerge"/> objects describing the a set of merges.</param>
        /// <param name="tfsPathParentBranch">The tfs Path Parent Branch.</param>
        /// <param name="tfsPathBranchToCreate">The tfs Path Branch To Create.</param>
        /// <param name="renameFromBranch"></param>
        /// <remarks>
        /// Each <see cref="ChangeType"/> uses the SourceChangeset, SourceItem, TargetChangeset, and TargetItem 
        /// properties with different semantics, depending on what it needs to describe, so the strategy to determine
        /// whether we are interested in a given ExtendedMerge summary depends on the SourceItem's <see cref="ChangeType"/>.
        /// </remarks>
        /// <returns>the <see cref="ChangesetSummary"/> of the changeset found.
        /// </returns>
        private static int GetRelevantChangesetBasedOnChangeType(IEnumerable<MergeInfo> merges, string tfsPathParentBranch, string tfsPathBranchToCreate, out string renameFromBranch)
        {
            renameFromBranch = null;
            var merge = merges.FirstOrDefault(m => m.SourceItem.Equals(tfsPathParentBranch, StringComparison.InvariantCultureIgnoreCase)
                && !m.TargetItem.Equals(tfsPathParentBranch, StringComparison.InvariantCultureIgnoreCase));

            if (merge == null)
            {
                merge = merges.FirstOrDefault(m => m.SourceItem.Equals(tfsPathBranchToCreate, StringComparison.InvariantCultureIgnoreCase)
                    && (m.TargetChangeType.HasFlag(ChangeType.Rename) || m.TargetChangeType.HasFlag(ChangeType.SourceRename)));
                if (merge == null)
                {
                    merge = merges.FirstOrDefault(m => m.SourceChangeType.HasFlag(ChangeType.Rename)
                        || m.SourceChangeType.HasFlag(ChangeType.SourceRename));
                }
                if (merge == null)
                    throw new GitTfsException("An unexpected error occured when trying to find the root changeset.\nFailed to find root changeset for " + tfsPathBranchToCreate + " branch in " + tfsPathParentBranch + " branch");
            }

            if (merge.SourceItem.Equals(tfsPathBranchToCreate, StringComparison.InvariantCultureIgnoreCase)
                && (merge.TargetChangeType.HasFlag(ChangeType.Rename)
                || merge.TargetChangeType.HasFlag(ChangeType.SourceRename)))
            {
                renameFromBranch = merge.TargetItem;
            }
            else if (merge.SourceChangeType.HasFlag(ChangeType.Rename)
                 || merge.SourceChangeType.HasFlag(ChangeType.SourceRename))
            {
                if (!merge.TargetItem.Equals(tfsPathBranchToCreate, StringComparison.InvariantCultureIgnoreCase))
                    renameFromBranch = merge.TargetItem;
                else
                    merge.SourceChangeType = ChangeType.Merge;
            }

            if (renameFromBranch != null)
            {
                Trace.WriteLine("Found C" + merge.TargetChangeset + " on branch " + merge.TargetItem);
                return merge.TargetChangeset;
            }
            if (merge.SourceChangeType.HasFlag(ChangeType.Branch)
                || merge.SourceChangeType.HasFlag(ChangeType.Merge)
                || merge.SourceChangeType.HasFlag(ChangeType.Add)
                || merge.SourceChangeType.HasFlag(ChangeType.Rollback))
            {
                Trace.WriteLine("Found C" + merge.SourceChangeset + " on branch " + merge.SourceItem);
                return merge.SourceChangeset;
            }
            throw new GitTfsException(
                "Don't know (yet) how to find the root changeset for an ExtendedMerge of type " +
                merge.SourceChangeType,
                new string[]
                            {
                                "Open an Issue on Github to notify the community that you need support for '" +
                                merge.SourceChangeType + "': https://github.com/git-tfs/git-tfs/issues"
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

        protected abstract string GetVsInstallDir();

        /// <summary>
        /// Help the TFS client find checkin policy assemblies.
        /// </summary>
        Assembly LoadFromVsFolder(object sender, ResolveEventArgs args)
        {
            Trace.WriteLine("Looking for assembly " + args.Name + " ...");
            string folderPath = Path.Combine(GetVsInstallDir(), "PrivateAssemblies");
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath) == false)
                return null;
            Trace.WriteLine("... loading " + args.Name + " from " + assemblyPath);
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        public override bool CanShowCheckinDialog { get { return true; } }

        public override long ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            return ShowCheckinDialog(_bridge.Unwrap<Workspace>(workspace),
                                     pendingChanges.Select(p => _bridge.Unwrap<PendingChange>(p)).ToArray(),
                                     checkedInfos.Select(c => _bridge.Unwrap<WorkItemCheckedInfo>(c)).ToArray(),
                                     checkinComment);
        }

        private long ShowCheckinDialog(Workspace workspace, PendingChange[] pendingChanges,
            WorkItemCheckedInfo[] checkedInfos, string checkinComment)
        {
            using (var parentForm = new ParentForm())
            {
                parentForm.Show();

                var dialog = Activator.CreateInstance(GetCheckinDialogType(), new object[] { workspace.VersionControlServer });

                return dialog.Call<int>("Show", parentForm.Handle, workspace, pendingChanges, pendingChanges,
                                        checkinComment, null, null, checkedInfos);
            }
        }

        private const string DialogAssemblyName = "Microsoft.TeamFoundation.VersionControl.ControlAdapter";

        private Type GetCheckinDialogType()
        {
            return GetDialogAssembly().GetType(DialogAssemblyName + ".CheckinDialog");
        }

        private Assembly GetDialogAssembly()
        {
            return Assembly.LoadFrom(GetDialogAssemblyPath());
        }

        private string GetDialogAssemblyPath()
        {
            return Path.Combine(GetVsInstallDir(), "PrivateAssemblies", DialogAssemblyName + ".dll");
        }
    }

}
