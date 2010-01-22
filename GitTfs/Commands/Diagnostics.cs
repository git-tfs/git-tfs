using System;
using System.Collections.Generic;
using System.IO;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("diagnostics")]
    public class Diagnostics : GitTfsCommand
    {
        private readonly TextWriter _stdout;

        public Diagnostics(TextWriter stdout)
        {
            _stdout = stdout;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(); }
        }

        public int Run(IList<string> args)
        {
            _stdout.WriteLine(ObjectFactory.WhatDoIHave());
            return GitTfsExitCodes.OK;
        }
    }
}
