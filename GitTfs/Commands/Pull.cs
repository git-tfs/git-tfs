using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        private TextWriter stdout;
        private bool _shouldRebase;

        public OptionSet OptionSet
        {
            get
            {
                return fetch.OptionSet
                            .Add("r|rebase", "Rebase your modifications on tfs changes", v => _shouldRebase = v != null);
            }
        }

        public Pull(Globals globals, Fetch fetch, TextWriter stdout)
        {
            this.fetch = fetch;
            this.globals = globals;
            this.stdout = stdout;
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
                    globals.WarnOnGitVersion(stdout);

                    if (globals.Repository.WorkingCopyHasUnstagedOrUncommitedChanges)
                    {
                        throw new GitTfsException("error: You have local changes; rebase-workflow only possible with clean working directory.")
                            .WithRecommendation("Try 'git stash' to stash your local changes and pull again.");
                    }
                    globals.Repository.CommandNoisy("rebase", "--preserve-merges", remote.RemoteRef);
                }
                else
                    globals.Repository.CommandNoisy("merge", remote.RemoteRef);
            }

            return retVal;
        }
    }
}
