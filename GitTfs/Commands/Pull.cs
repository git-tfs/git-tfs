using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("pull")]
    [Description("pull [options]")]
    [RequiresValidGitRepository]
    public class Pull : GitTfsCommand
    {
        #region GitTfsCommand Members
        private readonly Fetch fetch;
        private readonly Globals globals;

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(fetch); }
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
                var remoteRef = globals.Repository.ReadTfsRemote(remoteId);

                if (globals.Repository.WorkingCopyHasUnstagedOrUncommitedChanges)
                {
                    throw new GitTfsException(String.Format("error: You have local changes; cannot pull {0}.", remoteRef))
                        .WithRecommendation("Try 'git stash' to stash your local changes and pull again.");
                }

                globals.Repository.CommandNoisy("merge", remoteRef);
            }

            return retVal;
        }

        #endregion
    }
}
