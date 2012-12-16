using System;
using System.Collections.Generic;
using System.ComponentModel;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("pull")]
    [Description("pull [options]")]
    [RequiresValidGitRepository]
    public class Pull : GitTfsCommand
    {
        private readonly Fetch fetch;
        private readonly Globals globals;
        private bool _shouldRebase;

        public OptionSet OptionSet
        {
            get
            {
                return fetch.OptionSet
                            .Add("rebase", "rebase your modifications on tfs changes", v => _shouldRebase = v != null );
            }
        }

        public Pull(Globals globals, Fetch fetch)
        {
            this.fetch = fetch;
            this.globals = globals;
        }

        public int Run()
        {
            return Run(globals.RemoteId);
        }

        public int Run(string remoteId)
        {
            var retVal = fetch.Run(remoteId);

            if (retVal == 0)
            {
                var remote = globals.Repository.ReadTfsRemote(remoteId);
                if (_shouldRebase)
                {
                    if (globals.Repository.WorkingCopyHasUnstagedOrUncommitedChanges)
                    {
                        throw new GitTfsException("error: You have local changes; rebase-workflow only possible with clean working directory.")
                            .WithRecommendation("Try 'git stash' to stash your local changes and pull again.");
                    }
                    globals.Repository.CommandNoisy("rebase", remote.RemoteRef);
                }
                else
                    globals.Repository.CommandNoisy("merge", remote.RemoteRef);
            }

            return retVal;
        }
    }
}
