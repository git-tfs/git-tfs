using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("fetch")]
    [Description("fetch [options] [tfs-remote-id]")]
    public class Fetch : GitTfsCommand
    {
        private readonly FcOptions fcOptions;
        private readonly Globals globals;

        public Fetch(FcOptions fcOptions, Globals globals)
        {
            this.fcOptions = fcOptions;
            this.globals = globals;
        }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('r')]
        public int? revision { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("fetch-all")]
        public bool all { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('p')]
        public bool parent { get; set; }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(fcOptions); }
        }

        public int Run(IList<string> args)
        {
            if (all)
                args = globals.Repository.ReadAllRemotes();
            if (args.Count == 0)
                args = new[] {globals.RepositoryId};

            foreach(var repositoryId in args)
            {
                DoFetch(repositoryId);
            }
            return 0;
        }

        private void DoFetch(string repositoryId)
        {
            if(parent)
            {
                // lines 409-416
            }
            else
            {
                // lines 420-421
            }
        }
    }
}
