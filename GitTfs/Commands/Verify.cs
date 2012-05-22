using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("verify")]
    [RequiresValidGitRepository]
    public class Verify : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly TreeVerifier _verifier;

        public Verify(Globals globals, TreeVerifier verifier)
        {
            _globals = globals;
            _verifier = verifier;
        }

        public OptionSet OptionSet
        {
            get { return new OptionSet(); }
        }

        public int Run()
        {
            return Run("HEAD");
        }

        private int Run(string commitish)
        {
            // Warn, based on core.autocrlf or core.safecrlf value?
            //  -- autocrlf=true or safecrlf=true: TFS may have CRLF where git has LF
            var parents = _globals.Repository.GetLastParentTfsCommits(commitish);
            if(parents.IsEmpty())
                throw new GitTfsException("No TFS parents found to compare!");
            foreach(var parent in parents)
            {
                _verifier.Verify(parent);
            }
            return GitTfsExitCodes.OK;
        }
    }

    public class TreeVerifier
    {
        private readonly TextWriter _stdout;
        private readonly ITfsHelper _tfs;

        public TreeVerifier(TextWriter stdout, ITfsHelper tfs)
        {
            _stdout = stdout;
            _tfs = tfs;
        }

        public void Verify(TfsChangesetInfo changeset)
        {
            _stdout.WriteLine("Comparing TFS changeset " + changeset.ChangesetId + " to git commit " + changeset.GitCommit);
            var tfsTree = changeset.Remote.GetChangeset(changeset.ChangesetId).GetTree().ToDictionary(entry => entry.FullName.ToLowerInvariant().Replace("/",@"\"));
            var gitTree = changeset.Remote.Repository.GetCommit(changeset.GitCommit).GetTree().ToDictionary(entry => entry.Entry.Path.ToLowerInvariant());

            var all = tfsTree.Keys.Union(gitTree.Keys);
            var inBoth = tfsTree.Keys.Intersect(gitTree.Keys);
            var tfsOnly = tfsTree.Keys.Except(gitTree.Keys);
            var gitOnly = gitTree.Keys.Except(tfsTree.Keys);

            var foundDiff = false;
            foreach(var file in all.OrderBy(x => x))
            {
                if(tfsTree.ContainsKey(file))
                {
                    if(gitTree.ContainsKey(file))
                    {
                        if (Compare(tfsTree[file], gitTree[file]))
                            foundDiff = true;
                    }
                    else
                    {
                        _stdout.WriteLine("Only in TFS: " + tfsTree[file].FullName);
                        foundDiff = true;
                    }
                }
                else
                {
                    _stdout.WriteLine("Only in git: " + gitTree[file].FullName);
                    foundDiff = true;
                }
            }
            if(!foundDiff)
                _stdout.WriteLine("No differences!");
        }

        private bool Compare(TfsTreeEntry tfsTreeEntry, GitTreeEntry gitTreeEntry)
        {
            var different = false;
            if (tfsTreeEntry.FullName.Replace("/",@"\") != gitTreeEntry.FullName)
            {
                _stdout.WriteLine("Name case mismatch:");
                _stdout.WriteLine("  TFS: " + tfsTreeEntry.FullName);
                _stdout.WriteLine("  git: " + gitTreeEntry.FullName);
                different = true;
            }
            if(Hash(tfsTreeEntry) != Hash(gitTreeEntry))
            {
                _stdout.WriteLine(gitTreeEntry.FullName + " differs.");
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
