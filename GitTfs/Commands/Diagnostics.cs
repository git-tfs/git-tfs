using System;
using System.Collections.Generic;
using System.IO;
using NDesk.Options;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("diagnostics")]
    public class Diagnostics : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly IContainer _container;

        public Diagnostics(TextWriter stdout, IContainer container)
        {
            _stdout = stdout;
            _container = container;
        }

        public OptionSet OptionSet
        {
            get { return new OptionSet(); }
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(); }
        }

        public int Run()
        {
            _stdout.WriteLine(_container.WhatDoIHave());
            return GitTfsExitCodes.OK;
        }
    }
}
