using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("init")]
    public class Init : GitTfsCommand
    {
        public int Run(IEnumerable<string> args)
        {
            throw new System.NotImplementedException();
        }
    }
}
