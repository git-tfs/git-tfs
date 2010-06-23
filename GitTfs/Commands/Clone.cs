using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("clone")]
    [Description("clone [options] tfs-url repository-path <git-repository-path>")]
    public class Clone : GitTfsCommand
    {
        private readonly Fetch fetch;
        private readonly Init init;
        private readonly Globals globals;

        public Clone(Globals globals, Fetch fetch, Init init)
        {
            this.fetch = fetch;
            this.init = init;
            this.globals = globals;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(init, fetch); }
        }

        public int Run(IList<string> args)
        {
            var retVal = 0;
            retVal = init.Run(DeriveRepositoryDirectory(args));
            if (retVal == 0) retVal = fetch.Run(new List<string>());
            if (retVal == 0) globals.Repository.CommandNoisy("merge", globals.Repository.ReadAllTfsRemotes().First().RemoteRef);
            return retVal;
        }

        IList<string> DeriveRepositoryDirectory(IList<string> args)
        {
            if (args.Count == 2)
            {
                args = new List<string>(args);
                args.Add(Path.GetFileName(args[1]));
            }
            return args;
        }
    }
}
