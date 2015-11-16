using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkout")]
    [RequiresValidGitRepository]
    [Description("checkout changesetId [-b=branch_name]\n   ex: git-tfs checkout 2365\n       git-tfs checkout 2365 -b=bugfix_2365\n")]
    public class Checkout : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly TextWriter _stdout;

        public Checkout(Globals globals, TextWriter stdout)
        {
            _globals = globals;
            _stdout = stdout;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "b|branch=", "Name of the branch to create", v => BranchName = v },
                    { "n|dry-run", "Don't checkout the commit, just return commit sha", v => ReturnShaOnly = v != null },
                };
            }
        }


        protected string BranchName { get; set; }
        protected bool ReturnShaOnly { get; set; }

        public int Run(string id)
        {
            int changesetId;
            if(!int.TryParse(id, out changesetId))
                throw new GitTfsException("error: wrong format for changeset id...");
            var sha = _globals.Repository.FindCommitHashByChangesetId(changesetId);
            if (string.IsNullOrEmpty(sha))
                throw new GitTfsException("error: commit not found for this changeset id...");
            if (ReturnShaOnly)
            {
                _stdout.Write(sha);
                return GitTfsExitCodes.OK;
            }
            string commitishToCheckout = sha;
            if (!string.IsNullOrEmpty(BranchName))
            {
                BranchName = _globals.Repository.AssertValidBranchName(BranchName);
                if(!_globals.Repository.CreateBranch(BranchName.ToLocalGitRef(), sha))
                    throw new GitTfsException("error: can not create branch '" + BranchName + "'");
                _stdout.WriteLine("Branch '" + BranchName + "' created...");
                commitishToCheckout = BranchName;
            }
            if(!_globals.Repository.Checkout(commitishToCheckout))
                throw new GitTfsException("error: unable to checkout '" + commitishToCheckout + "' due to changes in your workspace!",
                    new List<string> { "commit or stash your changes before retrying..."});
            return GitTfsExitCodes.OK;
        }
    }
}
