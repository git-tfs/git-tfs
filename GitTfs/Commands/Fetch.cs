using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("fetch")]
    [Description("fetch [options] [tfs-remote-id]")]
    public class Fetch : GitTfsCommand
    {
        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('r')]
        public int? revision { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("fetch-all")]
        public bool all { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('p')]
        public bool parent { get; set; }

        private IGitTfs gitTfs;

        public int Run(IEnumerable<string> args)
        {
            var remoteId = args.FirstOrDefault() ?? GitTfs.DefaultRemoteId;
            gitTfs.FetchAll(remoteId);
            return 0;
        }
    }
}
