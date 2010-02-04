using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("pull")]
    [Description("pull [options] tfs-url repository-path")]
    [RequiresValidGitRepository]
    public class Pull : GitTfsCommand
    {
        #region GitTfsCommand Members
        private readonly GitTfsCommand fetch;
        private readonly Globals globals;

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(fetch); }
        }

        public Pull(Globals globals)
        {
            fetch = ObjectFactory.GetNamedInstance<GitTfsCommand>("fetch");
            this.globals = globals;
        }

        public int Run(IList<string> args)
        {
            var retVal = 0;
            retVal = fetch.Run(new List<string>());
            if (retVal == 0) globals.Repository.CommandNoisy("merge", "tfs/default");
            return retVal;
        }

        #endregion
    }
}
