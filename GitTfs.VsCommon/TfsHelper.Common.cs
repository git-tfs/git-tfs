using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using SEP.Extensions;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;
using StructureMap.Attributes;
using ChangeType = Microsoft.TeamFoundation.VersionControl.Client.ChangeType;
using IdentityNotFoundException = Microsoft.TeamFoundation.VersionControl.Client.IdentityNotFoundException;
using Microsoft.TeamFoundation.Build.Client;

namespace Sep.Git.Tfs.VsCommon
{
    public abstract class TfsHelperBase : ITfsHelper
    {
        protected readonly TfsApiBridge _bridge;
        private readonly IContainer _container;
        protected TfsTeamProjectCollection _server;
        private static bool _resolverInstalled;

        public TfsHelperBase(TfsApiBridge bridge, IContainer container)
        {
            _bridge = bridge;
            _container = container;
            if (!_resolverInstalled)
            {
                AppDomain.CurrentDomain.AssemblyResolve += LoadFromVsFolder;
                _resolverInstalled = true;
            }
        }

        [SetterProperty]
        public Janitor Janitor { get; set; }

        [SetterProperty]
        public ConfigProperties properties { get; set; }

        public string TfsClientLibraryVersion { get { return typeof(TfsTeamProjectCollection).Assembly.GetName().Version + " (MS)"; } }

        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool HasCredentials
        {
            get { return !string.IsNullOrEmpty(Username); }
        }

        public void EnsureAuthenticated()
        {
            if (string.IsNullOrEmpty(Url))
            {
                _server = null;
            }
            else
            {
                Uri uri;
                if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                {
                    // maybe it is not an Uri but instance name
                    var servers = RegisteredTfsConnections.GetConfigurationServers();
                    var registered = servers.FirstOrDefault(s => ((string.Compare(s.Name, Url, StringComparison.OrdinalIgnoreCase) == 0) ||
                                                                  (string.Compare(s.Name, Uri.EscapeDataString(Url), StringComparison.OrdinalIgnoreCase) == 0)));
                    if (registered == null)
                        throw new GitTfsException("Given tfs name is not correct URI and not found as a registered TFS instance");
                    uri = registered.Uri;
                }
                else
                {
                    uri = new Uri(Url);
                }

                _server = GetTfsCredential(uri);

                _server.EnsureAuthenticated();
            }
        }

        protected abstract TfsTeamProjectCollection GetTfsCredential(Uri uri);

        public abstract IIdentity GetIdentity(string username);

        protected NetworkCredential GetCredential()
        {
            if (!HasCredentials)
                return new NetworkCredential();
            var idx = Username.IndexOf('\\');
            if (idx >= 0)
            {
                string domain = Username.Substring(0, idx);
                string login = Username.Substring(idx + 1);
                return new NetworkCredential(login, Password, domain);
            }
            return new NetworkCredential(Username, Password);
        }

        protected T GetService<T>()
        {
            if (_server == null) EnsureAuthenticated();
            return (T)_server.GetService(typeof(T));
        }

        private VersionControlServer _versionControl;
        protected VersionControlServer VersionControl
        {
            get
            {
                if (_versionControl != null)
                    return _versionControl;
                _versionControl = GetService<VersionControlServer>();
                _versionControl.NonFatalError += NonFatalError;
                _versionControl.Getting += Getting;
                return _versionControl;
            }
        }

        private WorkItemStore WorkItems
        {
            get { return GetService<WorkItemStore>(); }
        }

        private void NonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Failure != null)
            {
                Trace.TraceInformation(e.Failure.Message);
                Trace.WriteLine("Failure: " + e.Failure.Inspect(), "tfs non-fatal error");
            }
            if (e.Exception != null)
            {
                Trace.TraceInformation(e.Exception.Message);
                Trace.WriteLine("Exception: " + e.Exception.Inspect(), "tfs non-fatal error");
            }
        }

        private void Getting(object sender, GettingEventArgs e)
        {
            Trace.WriteLine("get [C" + e.Version + "]" + e.ServerItem);
        }

        private ILinking _linking;
        private ILinking Linking
        {
            get { return _linking ?? (_linking = GetService<ILinking>()); }
        }

        public int BatchCount
        {
            get
            {
                return properties.BatchSize;
            }
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, int startVersion, IGitTfsRemote remote, int lastVersion = -1, bool byLots = false)
        {
            if (Is2008OrOlder)
            {
                foreach (var changeset in GetChangesetsForTfs2008(path, startVersion, remote))
                    yield return changeset;
                yield break;
            }

            var start = startVersion;
            Changeset[] changesets;
            var lastChangeset = lastVersion == -1 ? VersionSpec.Latest : new ChangesetVersionSpec(lastVersion);
            do
            {
                var startChangeset = new ChangesetVersionSpec(start);
                changesets = Retry.Do(() => VersionControl.QueryHistory(path, lastChangeset, 0, RecursionType.Full,
                    null, startChangeset, lastChangeset, BatchCount, true, true, true, true)
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
            } while (!byLots && changesets.Length == BatchCount);
        }

        public IEnumerable<ITfsChangeset> GetChangesetsForTfs2008(string path, int startVersion, IGitTfsRemote remote)
        {
            var changesets = VersionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
                                                                        null, new ChangesetVersionSpec(startVersion), VersionSpec.Latest, int.MaxValue,
                                                                        true, true, true)
                                                          .Cast<Changeset>().OrderBy(changeset => changeset.ChangesetId).ToArray();
            // don't take the enumerator produced by a foreach statement or a yield statement, as there are references
            // to the old (iterated) elements and thus the referenced changesets won't be disposed until all elements were iterated.
            for (int i = 0; i < changesets.Length; i++)
            {
                yield return BuildTfsChangeset(changesets[i], remote);
                changesets[i] = null;
            }
        }

        public virtual int FindMergeChangesetParent(string path, int targetChangeset, GitTfsRemote remote)
        {
            var targetVersion = new ChangesetVersionSpec(targetChangeset);
            var searchTo = targetVersion;
            var mergeInfo = VersionControl.QueryMerges(null, null, path, targetVersion, null, searchTo, RecursionType.Full);
            if (mergeInfo.Length == 0) return -1;
            return mergeInfo.Max(x => x.SourceVersion);
        }

        public bool Is2008OrOlder
        {
            get { return _server.ConfigurationServer == null; }
        }

        public bool CanGetBranchInformation
        {
            get { return !Is2008OrOlder; }
        }

        public IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            return VersionControl.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => b.Properties.ParentBranch == null)
                .Select(b => b.Properties.RootItem.Item);
        }

        public IEnumerable<IBranchObject> GetBranches(bool getAlsoDeletedBranches = false)
        {
            var branches = VersionControl.QueryRootBranchObjects(RecursionType.Full);
            if (getAlsoDeletedBranches)
                return _bridge.Wrap<WrapperForBranchObject, BranchObject>(branches);
            return _bridge.Wrap<WrapperForBranchObject, BranchObject>(branches.Where(b => !b.Properties.RootItem.IsDeleted));
        }

        public IList<RootBranch> GetRootChangesetForBranch(string tfsPathBranchToCreate, int lastChangesetIdToCheck = -1, string tfsPathParentBranch = null)
        {
            var rootBranches = new List<RootBranch>();
            GetRootChangesetForBranch(rootBranches, tfsPathBranchToCreate, lastChangesetIdToCheck, tfsPathParentBranch);
            return rootBranches;
        }

        private void GetRootChangesetForBranch(IList<RootBranch> rootBranches, string tfsPathBranchToCreate, int lastChangesetIdToCheck = -1, string tfsPathParentBranch = null)
        {
            Trace.WriteLine("Looking for root changeset for branch:" + tfsPathBranchToCreate);

            if (lastChangesetIdToCheck == -1)
                lastChangesetIdToCheck = int.MaxValue;

            try
            {
                if (!CanGetBranchInformation)
                {
                    Trace.WriteLine("Try TFS2008 compatibility mode...");
                    foreach (var rootBranch in GetRootChangesetForBranchForTfs2008(tfsPathBranchToCreate, lastChangesetIdToCheck, tfsPathParentBranch))
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
                    throw new GitTfsException("error : the branch you try to initialize '" + tfsPathBranchToCreate + "' is a root branch (e.g. has no parents).",
                        new List<string> { "Clone this branch from Tfs instead of trying to initialize it!\n   Command: git tfs clone " + Url + " " + tfsPathBranchToCreate });
                }

                tfsPathParentBranch = tfsParentBranch;
                Trace.WriteLine("Found parent branch : " + tfsPathParentBranch);


                try
                {
                    // This method now handles the scenario where a valid branch has been detected for migration but its
                    // root/branch changeset is *not* the first changeset in its history.
                    //
                    // This situation can occur when:
                    //
                    //  1) My project is created (e.g. $/MyProject/MyTrunk) (C1)
                    //  2) Work is done on $/MyProject/MyTrunk (C2)
                    //  3) A folder is created based on the contents of $/MyProject/MyTrunk without branching (e.g. $/MyProject/MyFeature) (C3)
                    //  4) Folder $/MyProject/MyFeature is deleted (C4)
                    //  5) Branch $/MyProject/MyFeature is created from $/MyProject/MyTrunk (C5)
                    //
                    // In this case, the code previously assumed C3 is the root changeset and would only check for merge history in it.
                    // Now, the code does not assume any given changeset is the branch root and instead crawls its history in
                    // batches to find the first changeset with merge history and assumes that changeset is the root.

                    const int batchSize = 100;

                    IEnumerable<MergeInfo> branchChangesetInTargetBranch = null;
                    for (var batchNumber = 1; branchChangesetInTargetBranch == null; batchNumber++)
                    {
                        var changesetsToRetrieve = batchNumber * batchSize;

                        var changesetEnumerable = VersionControl.QueryHistory(tfsPathBranchToCreate, VersionSpec.Latest, 0,
                            RecursionType.Full, null, null, null, changesetsToRetrieve, false, false, false, true).Cast<Changeset>();

                        if (batchNumber > 1)
                        {
                            changesetEnumerable = changesetEnumerable.Skip((batchNumber - 1) * batchSize).Take(batchSize);
                        }

                        // ToList'ed because inspecting the enumerable during debugging was resulting in TFS timeouts
                        var changesets = changesetEnumerable.ToList();

                        // If our batch has no results, there's nothing left to check; we're done.
                        if (!changesets.Any())
                        {
                            break;
                        }

                        foreach (var changeset in changesets)
                        {
                            var branchChangesetsInTargetBranchForBatch = GetMergeInfo(tfsPathBranchToCreate, tfsPathParentBranch, changeset.ChangesetId, lastChangesetIdToCheck);

                            if (branchChangesetsInTargetBranchForBatch.Any())
                            {
                                branchChangesetInTargetBranch = branchChangesetsInTargetBranchForBatch;
                                break;
                            }
                        }
                    }

                    if (branchChangesetInTargetBranch == null)
                    {
                        throw new GitTfsException("An unexpected error occurred when trying to find the root changeset.\nFailed to find first changeset for " + tfsPathBranchToCreate);
                    }

                    string renameFromBranch;
                    var rootChangesetInParentBranch =
                        GetRelevantChangesetBasedOnChangeType(branchChangesetInTargetBranch, tfsPathParentBranch, tfsPathBranchToCreate, out renameFromBranch);

                    var rootChangesetMergeInfo = branchChangesetInTargetBranch.First();

                    // If the merge info indicates our parent branch root changeset is the source, then our child changeset
                    // will be the target. Otherwise, they're swapped.
                    var rootChangesetInChildBranch = rootChangesetMergeInfo.SourceChangeset == rootChangesetInParentBranch ?
                        rootChangesetMergeInfo.TargetChangeset : rootChangesetMergeInfo.SourceChangeset;

                    var rootBranch = new RootBranch(rootChangesetInParentBranch, rootChangesetInChildBranch, tfsPathBranchToCreate);
                    AddNewRootBranch(rootBranches, rootBranch);

                    if (renameFromBranch != null)
                    {
                        Trace.WriteLine("Found original branch '" + renameFromBranch + "' (renamed in branch '" + tfsPathBranchToCreate + "')");
                        GetRootChangesetForBranch(rootBranches, renameFromBranch);
                    }
                }
                catch (VersionControlException)
                {
                    throw new GitTfsException("An unexpected error occurred when trying to find the root changeset.\nFailed to query history for " + tfsPathBranchToCreate);
                }
            }
            catch (FeatureNotSupportedException ex)
            {
                Trace.WriteLine(ex.Message);
                foreach (var rootBranch in GetRootChangesetForBranchForTfs2008(tfsPathBranchToCreate, -1, tfsPathParentBranch))
                {
                    AddNewRootBranch(rootBranches, rootBranch);
                }
            }
        }

        public IList<RootBranch> GetRootChangesetForBranchForTfs2008(string tfsPathBranchToCreate, int lastChangesetIdToCheck = -1, string tfsPathParentBranch = null)
        {
            Trace.WriteLine("TFS 2008 Compatible mode!");
            int firstChangesetIdOfParentBranch = 1;

            if (string.IsNullOrWhiteSpace(tfsPathParentBranch))
                throw new GitTfsException("This version of TFS Server doesn't permit to use this command :(\nTry using option '--parent-branch'...");

            if (lastChangesetIdToCheck == -1)
                lastChangesetIdToCheck = int.MaxValue;

            var changesetIdsFirstChangesetInMainBranch = VersionControl.GetMergeCandidates(tfsPathParentBranch, tfsPathBranchToCreate, RecursionType.Full)
                .Select(c => c.Changeset.ChangesetId).Where(c => c <= lastChangesetIdToCheck).FirstOrDefault();

            if (changesetIdsFirstChangesetInMainBranch == 0)
            {
                Trace.WriteLine("No changeset in main branch since branch done... (need only to find the last changeset in the main branch)");
                return new List<RootBranch> { new RootBranch(VersionControl.QueryHistory(tfsPathParentBranch, VersionSpec.Latest, 0,
                        RecursionType.Full, null, new ChangesetVersionSpec(firstChangesetIdOfParentBranch), VersionSpec.Latest,
                        1, false, false).Cast<Changeset>().First().ChangesetId, tfsPathBranchToCreate)};
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
                                null, new ChangesetVersionSpec(lowerBound), new ChangesetVersionSpec(upperBound), int.MaxValue, false,
                                false, false).Cast<Changeset>().Select(c => c.ChangesetId).ToList();
                if (firstBranchChangesetIds.Count != 0)
                    return new List<RootBranch> { new RootBranch(firstBranchChangesetIds.First(cId => cId < changesetIdsFirstChangesetInMainBranch), tfsPathBranchToCreate) };
                else
                {
                    if (upperBound == 1)
                    {
                        throw new GitTfsException("An unexpected error occurred when trying to find the root changeset.\nFailed to find a previous changeset to changeset n°" + changesetIdsFirstChangesetInMainBranch + " in the branch!!!");
                    }
                    upperBound = Math.Max(upperBound - step, 1);
                    lowerBound = Math.Max(upperBound - step, 1);
                }
            }
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

        /// <summary>
        /// Gets the relevant TFS changeset ID for the root changeset given a set
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
        /// <returns>Returns an integer representing the found changeset ID.</returns>
        private int GetRelevantChangesetBasedOnChangeType(IEnumerable<MergeInfo> merges, string tfsPathParentBranch, string tfsPathBranchToCreate, out string renameFromBranch)
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
                {
                    Trace.TraceWarning("warning: git-tfs was unable to find the root changeset (ie the last common commit) between the branch '"
                                      + tfsPathBranchToCreate + "' and its parent branch '" + tfsPathParentBranch + "'.\n"
                                      + "(Any help to add support of this special case is welcomed! Open an issue on https://github.com/git-tfs/git-tfs/issues )\n\n"
                                      + "To be able to continue to fetch the changesets from Tfs,\nplease enter the root changeset id between the branch '"
                                      + tfsPathBranchToCreate + "'\n and its parent branch '" + tfsPathParentBranch + "' by analysing the Tfs history...");
                    return AskForRootChangesetId();
                }
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
                || merge.SourceChangeType.HasFlag(ChangeType.Rollback)
                || merge.SourceChangeType.HasFlag(ChangeType.Delete)
                || merge.SourceChangeType.HasFlag(ChangeType.Undelete))
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

        private static void AddNewRootBranch(IList<RootBranch> rootBranches, RootBranch rootBranch)
        {
            if (rootBranches.Any())
                rootBranch.IsRenamedBranch = true;
            rootBranches.Insert(0, rootBranch);
        }

        private int AskForRootChangesetId()
        {
            int changesetId;
            while (true)
            {
                Trace.TraceInformation("Please specify the root changeset id (or 'exit' to stop the process):");
                var read = Console.ReadLine();
                if (read == "exit")
                    throw new GitTfsException("Exiting...(fetching stopped by user!)");
                if (!int.TryParse(read, out changesetId) || changesetId <= 0)
                    continue;
                return changesetId;
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
            int firstChangesetInBranchToCreate, int lastChangesetIdToCheck)
        {
            var mergedItemsToFirstChangesetInBranchToCreate = new List<MergeInfo>();
            var merges = VersionControl
                .TrackMerges(new int[] { firstChangesetInBranchToCreate },
                    new ItemIdentifier(tfsPathBranchToCreate),
                    new ItemIdentifier[] { new ItemIdentifier(tfsPathParentBranch), },
                    null)
                .OrderByDescending(x => x.SourceChangeset.ChangesetId);
            MergeInfo lastMerge = null;
            foreach (var extendedMerge in merges)
            {
                var sourceItem = extendedMerge.SourceItem.Item.ServerItem;
                var targetItem = extendedMerge.TargetItem != null ? extendedMerge.TargetItem.Item : null;
                var targetChangeType = extendedMerge.TargetItem != null ? extendedMerge.TargetItem.ChangeType : 0;
                if (extendedMerge.TargetChangeset.ChangesetId > lastChangesetIdToCheck)
                    continue;
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
                Trace.WriteLine("Merge: " + lastMerge);
            }
            return mergedItemsToFirstChangesetInBranchToCreate;
        }

        protected ITfsChangeset BuildTfsChangeset(Changeset changeset, IGitTfsRemote remote)
        {
            var tfsChangeset = _container.With<ITfsHelper>(this).With<IChangeset>(_bridge.Wrap<WrapperForChangeset, Changeset>(changeset)).GetInstance<TfsChangeset>();
            tfsChangeset.Summary = new TfsChangesetInfo { ChangesetId = changeset.ChangesetId, Remote = remote };

            if (HasWorkItems(changeset))
            {
                tfsChangeset.Summary.Workitems = changeset.WorkItems.Select(wi => new TfsWorkitem
                {
                    Id = wi.Id,
                    Title = wi.Title,
                    Description = wi.Description,
                    Url = Linking.GetArtifactUrl(wi.Uri.AbsoluteUri)
                });
            }
            tfsChangeset.Summary.CheckinNotes = changeset.CheckinNote.Values.Select(note => new TfsCheckinNote
            {
                Name = note.Name,
                Value = note.Value
            });
            tfsChangeset.Summary.PolicyOverrideComment = changeset.PolicyOverride.Comment;

            return tfsChangeset;
        }

        protected virtual bool HasWorkItems(Changeset changeset)
        {
            // This method wraps changeset.WorkItems, because
            // changeset.WorkItems might result to ConnectionException: TF26175: Team Foundation Core Services attribute 'AttachmentServerUrl' not found.
            // in this case assume that it is initialized to null
            // NB: in VS2011 a new property appeared (AssociatedWorkItems), which works correctly
            WorkItem[] result = null;
            try
            {
                result = Retry.Do(() => changeset.WorkItems);
            }
            catch (ConnectionException exception)
            {
                if (!exception.Message.StartsWith("TF26175:"))
                    throw;
            }

            return result != null && result.Length > 0;
        }

        private readonly Dictionary<string, Workspace> _workspaces = new Dictionary<string, Workspace>();

        public void WithWorkspace(string localDirectory, IGitTfsRemote remote, IEnumerable<Tuple<string, string>> mappings, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            Workspace workspace;
            if (!_workspaces.TryGetValue(remote.Id, out workspace))
            {
                Trace.WriteLine("Setting up a TFS workspace with subtrees at " + localDirectory);
                mappings = mappings.ToList(); // avoid iterating through the mappings more than once, and don't retry when this iteration raises an error.
                _workspaces.Add(remote.Id, workspace = Retry.Do(() =>
                {
                    var workingFolders = mappings.Select(x => new WorkingFolder(x.Item1, Path.Combine(localDirectory, x.Item2)));
                    return GetWorkspace(workingFolders.ToArray());
                }));
                Janitor.CleanThisUpWhenWeClose(() => TryToDeleteWorkspace(workspace));
            }
            var tfsWorkspace = _container.With("localDirectory").EqualTo(localDirectory)
                .With("remote").EqualTo(remote)
                .With("contextVersion").EqualTo(versionToFetch)
                .With("workspace").EqualTo(_bridge.Wrap<WrapperForWorkspace, Workspace>(workspace))
                .With("tfsHelper").EqualTo(this)
                .GetInstance<TfsWorkspace>();
            action(tfsWorkspace);
        }

        public void WithWorkspace(string localDirectory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            Trace.WriteLine("Setting up a TFS workspace at " + localDirectory);
            var workspace = Retry.Do(() => GetWorkspace(new WorkingFolder(remote.TfsRepositoryPath, localDirectory)));
            try
            {
                var tfsWorkspace = _container.With("localDirectory").EqualTo(localDirectory)
                    .With("remote").EqualTo(remote)
                    .With("contextVersion").EqualTo(versionToFetch)
                    .With("workspace").EqualTo(_bridge.Wrap<WrapperForWorkspace, Workspace>(workspace))
                    .With("tfsHelper").EqualTo(this)
                    .GetInstance<TfsWorkspace>();
                action(tfsWorkspace);
            }
            finally
            {
                TryToDeleteWorkspace(workspace);
            }
        }

        private Workspace GetWorkspace(params WorkingFolder[] folders)
        {
            var workspace = VersionControl.CreateWorkspace(GenerateWorkspaceName());
            try
            {
                SetWorkspaceMappingFolders(workspace, folders);
            }
            catch (MappingConflictException e)
            {
                try
                {
                    foreach (WorkingFolder folder in folders)
                        CleanupWorkspaces(folder.LocalItem);
                    SetWorkspaceMappingFolders(workspace, folders);
                }
                catch
                {
                    TryToDeleteWorkspace(workspace);
                    throw new GitTfsException(e.Message).WithRecommendation("Run 'git tfs cleanup-workspaces' to remove the workspace.");
                }
            }
            catch
            {
                TryToDeleteWorkspace(workspace);
                throw;
            }
            return workspace;
        }

        private static void SetWorkspaceMappingFolders(Workspace workspace, WorkingFolder[] folders)
        {
            foreach (WorkingFolder folder in folders)
                workspace.CreateMapping(folder);
        }

        private string GenerateWorkspaceName()
        {
            return "git-tfs-" + Guid.NewGuid();
        }

        public int ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            return ShowCheckinDialog(_bridge.Unwrap<Workspace>(workspace),
                                     pendingChanges.Select(p => _bridge.Unwrap<PendingChange>(p)).ToArray(),
                                     checkedInfos.Select(c => _bridge.Unwrap<WorkItemCheckedInfo>(c)).ToArray(),
                                     checkinComment);
        }

        private int ShowCheckinDialog(Workspace workspace, PendingChange[] pendingChanges,
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

        protected const string DialogAssemblyName = "Microsoft.TeamFoundation.VersionControl.ControlAdapter";

        private Type GetCheckinDialogType()
        {
            return GetDialogAssembly().GetType(DialogAssemblyName + ".CheckinDialog");
        }

        private Assembly GetDialogAssembly()
        {
            return Assembly.LoadFrom(GetDialogAssemblyPath());
        }

        protected virtual string GetDialogAssemblyPath()
        {
            return Path.Combine(GetVsInstallDir(), "PrivateAssemblies", DialogAssemblyName + ".dll");
        }

        public void CleanupWorkspaces(string workingDirectory)
        {
            // workingDirectory is the path to a TFS workspace managed by git-tfs.
            // By default, this will be something like `.git/tfs/default/workspace`.
            // If `git-tfs.workspace-dir` is set, workingDirectory will be that path.

            Trace.WriteLine("Looking for workspaces mapped to @\"" + workingDirectory + "\"...", "cleanup-workspaces");
            var workspace = VersionControl.TryGetWorkspace(workingDirectory);
            if (workspace != null)
            {
                Trace.WriteLine("Found mapping in workspace \"" + workspace.DisplayName + "\".", "cleanup-workspaces");
                if (workspace.Folders.Length == 1)
                {
                    // Normally, the workspace will have one mapping, mapped to the git-tfs
                    // workspace folder. In that case, we just delete the workspace.
                    Trace.TraceInformation("Removing workspace \"" + workspace.DisplayName + "\".");
                    workspace.Delete();
                }
                else
                {
                    // If something outside of git-tfs set up a workspace, the workspace
                    // might be set at a higher directory. If there is more than one mapping
                    // in the workspace, we only need to remove the one that includes the working
                    // directory that we want to set.
                    var fullWorkingDirectoryPath = Path.GetFullPath(workingDirectory);
                    foreach (var mapping in workspace.Folders.Where(f => fullWorkingDirectoryPath.StartsWith(Path.GetFullPath(f.LocalItem), StringComparison.CurrentCultureIgnoreCase)))
                    {
                        Trace.TraceInformation("Removing @\"" + mapping.LocalItem + "\" from workspace \"" + workspace.DisplayName + "\".");
                        workspace.DeleteMapping(mapping);
                    }
                }
            }
        }
        /// <summary>
        /// Method to help improve the process that deletes workspaces used by Git-TFS.
        /// The the delete fails, the process pauses for 5 seconds and retry 25 times before reporting failures.
        /// </summary>
        /// <param name="workspace"></param>
        /// <remarks>
        /// TFS randomly seems to report a workspace removed/deleted BUT subsequent calls by Git-TFS suggest that the delete hasn't actually been completed.
        /// This suggest that deletes may be queued and take a lower priority than other actions, especially if the TFS server is under load.
        /// </remarks>
        private void TryToDeleteWorkspace(Workspace workspace)
        {
            //  Try and ensure the client and TFS Server are synchronized.
            workspace.Refresh();

            //  When deleting a workspace we may need to allow the TFS server some time to complete existing processing or re-try the workspace delete.
            var deleteWsCompleted = Retry.Do(() => workspace.Delete(), TimeSpan.FromSeconds(5), 25);

            // Include trace information about the success of the TFS API that deletes the workspace.
            Trace.WriteLine(string.Format(deleteWsCompleted ? "TFS Workspace '{0}' was removed." : "TFS Workspace '{0}' could not be removed", workspace.DisplayName));
        }

        public bool HasShelveset(string shelvesetName)
        {
            var matchingShelvesets = VersionControl.QueryShelvesets(shelvesetName, GetAuthenticatedUser());
            return matchingShelvesets != null && matchingShelvesets.Length > 0;
        }

        protected string GetAuthenticatedUser()
        {
            return VersionControl.AuthorizedUser;
        }

        public bool CanShowCheckinDialog { get { return true; } }

        public ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName)
        {
            shelvesetOwner = shelvesetOwner == "all" ? null : (shelvesetOwner ?? GetAuthenticatedUser());
            var shelvesets = VersionControl.QueryShelvesets(shelvesetName, shelvesetOwner);
            if (shelvesets.Length != 1)
            {
                throw new GitTfsException("Unable to find " + shelvesetOwner + "'s shelveset \"" + shelvesetName + "\" (" + shelvesets.Length + " matches).")
                    .WithRecommendation("Try providing the shelveset owner.");
            }
            var shelveset = shelvesets.First();

            var itemSpec = new ItemSpec(remote.TfsRepositoryPath, RecursionType.Full);
            var change = VersionControl.QueryShelvedChanges(shelveset, new ItemSpec[] { itemSpec }).SingleOrDefault();
            if (change == null)
            {
                throw new GitTfsException("There is no changes in this shelveset that apply to the current tfs remote.")
                    .WithRecommendation("Try to apply the shelveset on another remote.");
            }
            var wrapperForVersionControlServer =
                _bridge.Wrap<WrapperForVersionControlServer, VersionControlServer>(VersionControl);
            // TODO - containerify this (no `new`)!
            var fakeChangeset = new Unshelveable(shelveset, change, wrapperForVersionControlServer, _bridge);
            var tfsChangeset = new TfsChangeset(remote.Tfs, fakeChangeset, null) { Summary = new TfsChangesetInfo { Remote = remote } };
            return tfsChangeset;
        }

        public int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote)
        {
            var shelvesetOwner = shelveList.Owner == "all" ? null : (shelveList.Owner ?? GetAuthenticatedUser());
            IEnumerable<Shelveset> shelvesets;
            try
            {
                shelvesets = VersionControl.QueryShelvesets(null, shelvesetOwner);
            }
            catch (IdentityNotFoundException)
            {
                Trace.TraceInformation("User '{0}' not found", shelveList.Owner);
                return GitTfsExitCodes.InvalidArguments;
            }
            if (shelvesets.Empty())
            {
                Trace.TraceInformation("No changesets found.");
                return GitTfsExitCodes.OK;
            }

            string sortBy = shelveList.SortBy;
            if (sortBy != null)
            {
                switch (sortBy.ToLowerInvariant())
                {
                    case "date":
                        shelvesets = shelvesets.OrderBy(s => s.CreationDate);
                        break;
                    case "owner":
                        shelvesets = shelvesets.OrderBy(s => s.OwnerName).ThenBy(s => s.CreationDate);
                        break;
                    case "name":
                        shelvesets = shelvesets.OrderBy(s => s.Name);
                        break;
                    case "comment":
                        shelvesets = shelvesets.OrderBy(s => s.Comment);
                        break;
                    default:
                        Trace.TraceInformation("ERROR: sorting criteria '{0}' is invalid. Possible values are: date, owner, name, comment", sortBy);
                        return GitTfsExitCodes.InvalidArguments;
                }
            }
            else
                shelvesets = shelvesets.OrderBy(s => s.CreationDate);

            if (shelveList.FullFormat)
                WriteShelvesetsToStdoutDetailed(shelvesets);
            else
                WriteShelvesetsToStdout(shelvesets);
            return GitTfsExitCodes.OK;
        }

        private void WriteShelvesetsToStdout(IEnumerable<Shelveset> shelvesets)
        {
            foreach (var shelveset in shelvesets)
            {
                Trace.TraceInformation("{0,-22} {1,-20}", shelveset.OwnerName, shelveset.Name);
            }
        }

        private void WriteShelvesetsToStdoutDetailed(IEnumerable<Shelveset> shelvesets)
        {
            foreach (var shelveset in shelvesets)
            {
                Trace.TraceInformation("Name   : {0}", shelveset.Name);
                Trace.TraceInformation("Owner  : {0}", shelveset.OwnerName);
                Trace.TraceInformation("Date   : {0:g}", shelveset.CreationDate);
                Trace.TraceInformation("Comment: {0}", shelveset.Comment);
                Trace.TraceInformation(string.Empty);
            }
        }

        #region Fake classes for unshelve

        private class Unshelveable : IChangeset
        {
            private readonly Shelveset _shelveset;
            private readonly PendingSet _pendingSet;
            private readonly IVersionControlServer _versionControlServer;
            private readonly TfsApiBridge _bridge;
            private readonly IChange[] _changes;

            public Unshelveable(Shelveset shelveset, PendingSet pendingSet, IVersionControlServer versionControlServer, TfsApiBridge bridge)
            {
                _shelveset = shelveset;
                _versionControlServer = versionControlServer;
                _bridge = bridge;
                _pendingSet = pendingSet;
                _changes = _pendingSet.PendingChanges.Select(x => new UnshelveChange(x, _bridge, versionControlServer)).Cast<IChange>().ToArray();
            }

            public IChange[] Changes
            {
                get { return _changes; }
            }

            public string Committer
            {
                get { return _pendingSet.OwnerName; }
            }

            public DateTime CreationDate
            {
                get { return _shelveset.CreationDate; }
            }

            public string Comment
            {
                get { return _shelveset.Comment; }
            }

            public int ChangesetId
            {
                get { return -1; }
            }

            public IVersionControlServer VersionControlServer
            {
                get { return _versionControlServer; }
            }

            public void Get(ITfsWorkspace workspace, IEnumerable<IChange> changes, Action<Exception> ignorableErrorHandler)
            {
                foreach (var change in changes)
                {
                    ignorableErrorHandler.Catch(() =>
                    {
                        var item = (UnshelveItem)change.Item;
                        item.Get(workspace);
                    });
                }
            }
        }

        private class UnshelveChange : IChange
        {
            private readonly PendingChange _pendingChange;
            private readonly TfsApiBridge _bridge;
            private readonly UnshelveItem _fakeItem;

            public UnshelveChange(PendingChange pendingChange, TfsApiBridge bridge, IVersionControlServer versionControlServer)
            {
                _pendingChange = pendingChange;
                _bridge = bridge;
                _fakeItem = new UnshelveItem(_pendingChange, _bridge, versionControlServer);
            }

            public TfsChangeType ChangeType
            {
                get { return _bridge.Convert<TfsChangeType>(_pendingChange.ChangeType); }
            }

            public IItem Item
            {
                get { return _fakeItem; }
            }
        }

        private class UnshelveItem : IItem
        {
            private readonly PendingChange _pendingChange;
            private readonly TfsApiBridge _bridge;
            private readonly IVersionControlServer _versionControlServer;
            private long _contentLength = -1;

            public UnshelveItem(PendingChange pendingChange, TfsApiBridge bridge, IVersionControlServer versionControlServer)
            {
                _pendingChange = pendingChange;
                _bridge = bridge;
                _versionControlServer = versionControlServer;
            }

            public IVersionControlServer VersionControlServer
            {
                get { return _versionControlServer; }
            }

            public int ChangesetId
            {
                get
                {
                    // some operations like applying rename gets previous item state
                    // via looking at version of item minus 1. So will try to emulate
                    // that this shelve is real revision.
                    return _pendingChange.Version + 1;
                }
            }

            public string ServerItem
            {
                get { return _pendingChange.ServerItem; }
            }

            public int DeletionId
            {
                get { return _pendingChange.DeletionId; }
            }

            public TfsItemType ItemType
            {
                get { return _bridge.Convert<TfsItemType>(_pendingChange.ItemType); }
            }

            public int ItemId
            {
                get { return _pendingChange.ItemId; }
            }

            public long ContentLength
            {
                get
                {
                    if (_contentLength < 0)
                        throw new InvalidOperationException("You can't query ContentLength before downloading the file");
                    // It is not great solution, but at least makes the contract explicit.
                    // We can't actually save downloaded file in this class, because if nobody asks
                    // for it - we won't know when it is safe to delete it and it will stay in the
                    // system forever, which is bad. Implementing finalizer to delete file is also bad solution:
                    // suppose process was killed in the middle of many-megabyte operation on thousands of files
                    // if we delete them as soon as they are not used - only current file will remain. Otherwise
                    // all of them.
                    // With this exception at least it would be evident asap that something went wrong, so we could fix it.
                    return _contentLength;
                }
            }

            public TemporaryFile DownloadFile()
            {
                var temp = new TemporaryFile();
                _pendingChange.DownloadShelvedFile(temp);
                _contentLength = new FileInfo(temp).Length;
                return temp;
            }

            public void Get(ITfsWorkspace workspace)
            {
                _pendingChange.DownloadShelvedFile(workspace.GetLocalItemForServerItem(_pendingChange.ServerItem));
            }
        }

        #endregion

        public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
        {
            var shelveset = new Shelveset(_bridge.Unwrap<Workspace>(workspace).VersionControlServer, shelvesetName, workspace.OwnerName);
            return _bridge.Wrap<WrapperForShelveset, Shelveset>(shelveset);
        }

        public Changeset GetLatestChangeset(IGitTfsRemote remote, bool includeChanges)
        {
            var history = VersionControl.QueryHistory(remote.TfsRepositoryPath, VersionSpec.Latest, 0,
                                                      RecursionType.Full, null, null, VersionSpec.Latest, 1, includeChanges, false,
                                                      false).Cast<Changeset>().ToList();

            if (history.Empty())
                throw new GitTfsException("error: remote TFS repository path was not found");

            return history.Single();
        }

        public ITfsChangeset GetLatestChangeset(IGitTfsRemote remote)
        {
            return BuildTfsChangeset(GetLatestChangeset(remote, true), remote);
        }

        public int GetLatestChangesetId(IGitTfsRemote remote)
        {
            return GetLatestChangeset(remote, false).ChangesetId;
        }

        public IChangeset GetChangeset(int changesetId)
        {
            return _bridge.Wrap<WrapperForChangeset, Changeset>(VersionControl.GetChangeset(changesetId));
        }

        public ITfsChangeset GetChangeset(int changesetId, IGitTfsRemote remote)
        {
            return BuildTfsChangeset(VersionControl.GetChangeset(changesetId), remote);
        }

        public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            return
                GetWorkItemInfosHelper<IWorkItemCheckinInfo, WrapperForWorkItemCheckinInfo, WorkItemCheckinInfo>(
                    workItems, checkinAction, GetWorkItemInfo);
        }

        public IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            return
                GetWorkItemInfosHelper<IWorkItemCheckedInfo, WrapperForWorkItemCheckedInfo, WorkItemCheckedInfo>(
                    workItems, checkinAction, GetWorkItemCheckedInfo);
        }

        public ICheckinNote CreateCheckinNote(Dictionary<string, string> checkinNotes)
        {
            if (checkinNotes.IsEmpty())
            {
                return null;
            }

            var index = 0;
            var values = new CheckinNoteFieldValue[checkinNotes.Count];
            foreach (var pair in checkinNotes)
            {
                values[index++] = new CheckinNoteFieldValue(pair.Key, pair.Value);
            }

            return _bridge.Wrap<WrapperForCheckinNote, CheckinNote>(new CheckinNote(values));
        }

        private IEnumerable<TInterface> GetWorkItemInfosHelper<TInterface, TWrapper, TInstance>(
            IEnumerable<string> workItems,
            TfsWorkItemCheckinAction checkinAction,
            Func<string, WorkItemCheckinAction, TInstance> func
            )
            where TWrapper : class
        {
            return (from workItem in workItems
                    select _bridge.Wrap<TWrapper, TInstance>(
                        func(workItem, _bridge.Convert<WorkItemCheckinAction>(checkinAction))))
                .Cast<TInterface>();
        }

        private WorkItemCheckinInfo GetWorkItemInfo(string workItem, WorkItemCheckinAction checkinAction)
        {
            return new WorkItemCheckinInfo(WorkItems.GetWorkItem(Convert.ToInt32(workItem)), checkinAction);
        }

        private static WorkItemCheckedInfo GetWorkItemCheckedInfo(string workitem, WorkItemCheckinAction checkinAction)
        {
            return new WorkItemCheckedInfo(Convert.ToInt32(workitem), true, checkinAction);
        }

        public IEnumerable<TfsLabel> GetLabels(string tfsPathBranch, string nameFilter = null)
        {
            foreach (var labelDefinition in VersionControl.QueryLabels(nameFilter, tfsPathBranch, null, false, tfsPathBranch, VersionSpec.Latest))
            {
                var label = VersionControl.QueryLabels(labelDefinition.Name, tfsPathBranch, null, true, tfsPathBranch, VersionSpec.Latest).FirstOrDefault();
                if (label == null)
                {
                    throw new GitTfsException("error: data for the label '" + labelDefinition.Name + "' can't be loaded!");
                }
                var tfsLabel = new TfsLabel
                {
                    Id = label.LabelId,
                    Name = label.Name,
                    Comment = label.Comment,
                    Owner = label.OwnerName,
                    Date = label.LastModifiedDate,
                };
                foreach (var item in label.Items)
                {
                    if (item.ServerItem.StartsWith(tfsPathBranch))
                    {
                        if (item.ChangesetId > tfsLabel.ChangesetId)
                        {
                            tfsLabel.ChangesetId = item.ChangesetId;
                        }
                    }
                    else
                    {
                        tfsLabel.IsTransBranch = true;
                    }
                }
                yield return tfsLabel;
            }
        }

        public void CreateBranch(string sourcePath, string targetPath, int changesetId, string comment = null)
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

        public void CreateTfsRootBranch(string projectName, string mainBranch, string gitRepositoryPath, bool createTeamProjectFolder)
        {
            var projectPath = "$/" + projectName;
            var directoryForBranch = Path.Combine(gitRepositoryPath, mainBranch);
            Workspace workspace = null;
            try
            {
                if (!VersionControl.ServerItemExists(projectPath, ItemType.Any))
                {
                    if (createTeamProjectFolder)
                        VersionControl.CreateTeamProjectFolder(new TeamProjectFolderOptions(projectName));
                    else
                        throw new GitTfsException("error: the team project folder '" + projectPath + "' doesn't exist!",
                            new List<string>()
                                {
                                    "Verify that the name of the project '" + projectName +"' is well spelled",
                                    "Create the team project folder in TFS before (recommended)",
                                    "Use the flag '--create-project-folder' to create the team project folder during the process"
                                });
                }

                workspace = GetWorkspace(new WorkingFolder(projectPath, gitRepositoryPath));
                if (!Directory.Exists(directoryForBranch))
                    Directory.CreateDirectory(directoryForBranch);
                workspace.PendAdd(directoryForBranch);
                var changes = workspace.GetPendingChanges();
                if (!changes.Any())
                    return;
                workspace.CheckIn(changes, "Creation project folder '" + mainBranch + "'");
                ConvertFolderIntoBranch(projectPath + "/" + mainBranch);
            }
            catch (GitTfsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GitTfsException("error: Unable to create project folder!", ex);
            }
            finally
            {
                if (Directory.Exists(directoryForBranch))
                    Directory.Delete(directoryForBranch);
                if (workspace != null)
                    workspace.DeleteMapping(workspace.GetWorkingFolderForLocalItem(gitRepositoryPath));
            }
        }

        public bool IsExistingInTfs(string path)
        {
            return VersionControl.ServerItemExists(path, ItemType.Any);
        }

        protected void ConvertFolderIntoBranch(string tfsRepositoryPath)
        {
            VersionControl.CreateBranchObject(new BranchProperties(new ItemIdentifier(tfsRepositoryPath)));
        }

        protected abstract string GetVsInstallDir();

        /// <summary>
        /// Help the TFS client find checkin policy assemblies.
        /// </summary>
        private Assembly LoadFromVsFolder(object sender, ResolveEventArgs args)
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

        protected string TryGetUserRegString(string path, string name)
        {
            return TryGetRegString(Registry.CurrentUser, path, name);
        }

        protected string TryGetRegString(string path, string name)
        {
            return TryGetRegString(Registry.LocalMachine, path, name);
        }

        protected string TryGetRegString(RegistryKey registryKey, string path, string name)
        {
            try
            {
                Trace.WriteLine("Trying to get " + registryKey.Name + "\\" + path + "|" + name);
                var key = registryKey.OpenSubKey(path);
                if (key != null)
                {
                    return key.GetValue(name) as string;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to get registry value " + registryKey.Name + "\\" + path + "|" + name + ": " + e);
            }
            return null;
        }

        /// <summary>
        /// Try to get the value of a key beginning with the name 'startOfName'
        /// and present in the tree path 'path' in the 'Current user' registry root.
        ///
        /// ex:
        ///
        /// The value "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions" for parameter 'startOfName'
        /// will find "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions,14.0.24712"
        /// and "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions,14.0.23102"
        /// </summary>
        /// <param name="path">path in the registry tree</param>
        /// <param name="startOfName">start of the name of the key to find</param>
        /// <returns>the value corresponding to the key found</returns>
        protected string TryGetUserRegStringStartingWithName(string path, string startOfName)
        {
            return TryGetRegStringStartingWithName(Registry.CurrentUser, path, startOfName);
        }

        /// <summary>
        /// Try to get the value of a key beginning with the name 'startOfName'
        /// and present in the tree path 'path' in the 'Local machine' registry root
        /// ex:
        /// The value "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions" for parameter 'startOfName'
        /// will find "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions,14.0.24712"
        /// and "Microsoft.VisualStudio.TeamFoundation.TeamExplorer.Extensions,14.0.23102"
        /// </summary>
        /// <param name="path">path in the registry tree</param>
        /// <param name="startOfName">start of the name of the key to find</param>
        /// <returns>the value corresponding to the key found</returns>
        protected string TryGetRegStringStartingWithName(string path, string startOfName)
        {
            return TryGetRegStringStartingWithName(Registry.LocalMachine, path, startOfName);
        }

        protected string TryGetRegStringStartingWithName(RegistryKey registryKey, string path, string startOfName)
        {
            try
            {
                Trace.WriteLine("Trying to get " + registryKey.Name + "\\" + path + "| starting with:" + startOfName);
                var key = registryKey.OpenSubKey(path);
                if (key != null)
                {
                    var keyFound = key.GetValueNames().FirstOrDefault(k => k.StartsWith(startOfName));
                    Trace.WriteLine("Found registry key:" + keyFound);
                    return key.GetValue(keyFound) as string;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to get registry value " + registryKey.Name + "\\" + path + "| starting with:" + startOfName + ": " + e);
            }
            return null;
        }

        public int QueueGatedCheckinBuild(Uri buildDefinitionUri, string buildDefinitionName, string shelvesetName, string checkInTicket)
        {
            var buildServer = (IBuildServer)_server.GetService(typeof(IBuildServer));

            var buildRequest = buildServer.CreateBuildRequest(buildDefinitionUri);
            buildRequest.ShelvesetName = shelvesetName;
            buildRequest.Reason = BuildReason.CheckInShelveset;
            buildRequest.GatedCheckInTicket = checkInTicket;

            Trace.TraceInformation("Launching build '" + buildDefinitionName + "' to validate your shelveset...");
            var queuedBuild = buildServer.QueueBuild(buildRequest);

            Trace.TraceInformation("Waiting for gated check-in build result...");
            do
            {
                Console.Write(".");
                System.Threading.Thread.Sleep(5000);
                queuedBuild.Refresh(QueryOptions.Definitions);
            } while (queuedBuild.Build == null || !queuedBuild.Build.BuildFinished);
            Trace.TraceInformation(string.Empty);

            var build = GetSpecificBuildFromQueuedBuild(queuedBuild, shelvesetName);
            if (build.Status == BuildStatus.Succeeded)
            {
                Trace.TraceInformation("Build was successful! Your changes have been checked in.");
                return VersionControl.GetLatestChangesetId();
            }
            else
            {
                throw new GitTfsException("The gated check-in has failed! Your changeset is rejected!");
            }
        }

        public void DeleteShelveset(IWorkspace workspace, string shelvesetName)
        {
            VersionControl.DeleteShelveset(shelvesetName, workspace.OwnerName);
        }

        protected virtual IBuildDetail GetSpecificBuildFromQueuedBuild(IQueuedBuild queuedBuild, string shelvesetName)
        {
            return queuedBuild.Build;
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
            var temp = new TemporaryFile();
            try
            {
                _bridge.Unwrap<Item>(item).DownloadFile(temp);
                return temp;
            }
            catch (Exception)
            {
                Trace.WriteLine(string.Format("Something went wrong downloading \"{0}\" in changeset {1}", item.ServerItem, item.ChangesetId));
                temp.Dispose();
                throw;
            }
        }
    }
}
