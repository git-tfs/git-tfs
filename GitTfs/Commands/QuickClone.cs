using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("quick-clone")]
    [Description("quick-clone [options] tfs-url repository-path <git-repository-path>")]
    public class QuickClone : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly Init _init;

        public QuickClone(Globals globals, Init init)
        {
            _globals = globals;
            _init = init;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(_init); }
        }

        public int Run(IList<string> args)
        {
            var retVal = 0;
            retVal = _init.Run(Clone.DeriveRepositoryDirectory(args));
            //if (retVal == 0) retVal = fetch.Run(new List<string>());
            if (retVal == 0) _globals.Repository.CommandNoisy("merge", _globals.Repository.ReadAllTfsRemotes().First().RemoteRef);
            return retVal;
        }
    }
}
