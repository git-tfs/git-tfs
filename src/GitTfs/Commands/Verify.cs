using System.Security.Cryptography;
using NDesk.Options;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using StructureMap;
using System.ComponentModel;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("verify")]
    [RequiresValidGitRepository]
    [Description("verify [options] [commitish]\n   ex: git-tfs verify\n       git-tfs verify 889ad74c162\n       git-tfs verify tfs/mybranch\n       git-tfs verify --all")]
    public class Verify : GitTfsCommand
    {
        private readonly RemoteOptions _remoteOptions;
        private readonly Help _helper;
        private readonly Globals _globals;
        private readonly TreeVerifier _verifier;

        public Verify(Globals globals, TreeVerifier verifier, Help helper, RemoteOptions remoteOptions)
        {
            _globals = globals;
            _verifier = verifier;
            _helper = helper;
            _remoteOptions = remoteOptions;
        }

        public OptionSet OptionSet => new OptionSet()
                {
                    { "ignore-path-case-mismatch", "Ignore the case mismatch in the path when comparing the files.",
                        v => IgnorePathCaseMismatch = v != null },
                    { "all", "Verify all the tfs remotes",
                        v => VerifyAllRemotes = v != null },
                }.Merge(_remoteOptions.OptionSet);

        public bool VerifyAllRemotes { get; set; }

        public bool IgnorePathCaseMismatch { get; set; }

        public int Run()
        {
            if (!VerifyAllRemotes)
                return Run("HEAD");
            int foundDiff = GitTfsExitCodes.OK;
            foreach (var remote in _globals.Repository.ReadAllTfsRemotes())
            {
                Trace.TraceInformation("Verifying remote '{0}' => '{1}' ...", remote.Id, remote.TfsRepositoryPath);
                foundDiff = Math.Max(foundDiff, RunFromCommitish(remote.RemoteRef));
            }
            return foundDiff;
        }

        public int Run(string commitish)
        {
            if (VerifyAllRemotes)
            {
                _helper.Run(this);
                return GitTfsExitCodes.Help;
            }
            return RunFromCommitish(commitish);
        }

        private int RunFromCommitish(string commitish)
        {
            // Warn, based on core.autocrlf or core.safecrlf value?
            //  -- autocrlf=true or safecrlf=true: TFS may have CRLF where git has LF
            var parents = _globals.Repository.GetLastParentTfsCommits(commitish);
            if (parents.IsNullOrEmpty())
                throw new GitTfsException("No TFS parents found to compare!");
            int foundDiff = GitTfsExitCodes.OK;
            foreach (var parent in parents)
            {
                foundDiff = Math.Max(foundDiff, _verifier.Verify(parent, IgnorePathCaseMismatch));
            }
            return foundDiff;
        }
    }

    public class TreeVerifier
    {
        private readonly ITfsHelper _tfs;

        public TreeVerifier(ITfsHelper tfs)
        {
            _tfs = tfs;
        }

        public int Verify(TfsChangesetInfo changeset, bool ignorePathCaseMismatch)
        {
            Trace.TraceInformation("Comparing TFS changeset " + changeset.ChangesetId + " to git commit " + changeset.GitCommit);
            var tfsTree = changeset.Remote.GetChangeset(changeset.ChangesetId).GetTree().ToDictionary(entry => entry.FullName.ToLowerInvariant());
            var gitTree = changeset.Remote.Repository.GetCommit(changeset.GitCommit).GetTree().ToDictionary(entry => entry.Entry.Path.ToLowerInvariant());

            var all = tfsTree.Keys.Union(gitTree.Keys);
            var inBoth = tfsTree.Keys.Intersect(gitTree.Keys);
            var tfsOnly = tfsTree.Keys.Except(gitTree.Keys);
            var gitOnly = gitTree.Keys.Except(tfsTree.Keys);

            var foundDiff = GitTfsExitCodes.OK;
            foreach (var file in all.OrderBy(x => x))
            {
                if (tfsTree.ContainsKey(file))
                {
                    if (gitTree.ContainsKey(file))
                    {
                        if (Compare(tfsTree[file], gitTree[file], ignorePathCaseMismatch))
                            foundDiff = Math.Max(foundDiff, GitTfsExitCodes.VerifyContentMismatch);
                    }
                    else
                    {
                        Trace.TraceInformation("Only in TFS: " + tfsTree[file].FullName);
                        foundDiff = Math.Max(foundDiff, GitTfsExitCodes.VerifyFileMissing);
                    }
                }
                else
                {
                    Trace.TraceInformation("Only in git: " + gitTree[file].FullName);
                    foundDiff = Math.Max(foundDiff, GitTfsExitCodes.VerifyFileMissing);
                }
            }
            if (foundDiff == GitTfsExitCodes.OK)
                Trace.TraceInformation("No differences!");
            return foundDiff;
        }

        private bool Compare(TfsTreeEntry tfsTreeEntry, GitTreeEntry gitTreeEntry, bool ignorePathCaseMismatch)
        {
            var different = false;
            if (!ignorePathCaseMismatch && tfsTreeEntry.FullName != gitTreeEntry.FullName)
            {
                Trace.TraceInformation("Name case mismatch:");
                Trace.TraceInformation("  TFS: " + tfsTreeEntry.FullName);
                Trace.TraceInformation("  git: " + gitTreeEntry.FullName);
                different = true;
            }
            if (Hash(tfsTreeEntry) != Hash(gitTreeEntry))
            {
                Trace.TraceInformation(gitTreeEntry.FullName + " differs.");
                different = true;
            }
            return different;
        }

        private string Hash(ITreeEntry treeEntry)
        {
            using (var stream = treeEntry.OpenRead())
                return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(stream));
        }
    }
}
