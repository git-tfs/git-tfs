using NDesk.Options;
using StructureMap;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("diagnostics")]
    public class Diagnostics : GitTfsCommand
    {
        private readonly IContainer _container;

        public Diagnostics(IContainer container)
        {
            _container = container;
        }

        public OptionSet OptionSet => new OptionSet();

        public int Run()
        {
            Trace.TraceInformation(_container.WhatDoIHave());
            return GitTfsExitCodes.OK;
        }
    }
}
