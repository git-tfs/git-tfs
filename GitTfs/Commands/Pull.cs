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

        public OptionSet OptionSet
        {
            get { return fetch.OptionSet; }
        }

        public IEnumerable<CommandLine.OptParse.IOptionResults> ExtraOptions
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
                var remote = globals.Repository.ReadTfsRemote(remoteId);
                globals.Repository.CommandNoisy("merge", remote.RemoteRef);
            }

            return retVal;
        }
    }
}
