using System.Collections.Generic;
using System.ComponentModel;
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

        public int Run(IList<string> args)
        {
            var retVal = 0;
            retVal = fetch.Run();
            if (retVal == 0) globals.Repository.CommandNoisy("merge", "tfs/default");
            return retVal;
        }

        #endregion
    }
}
