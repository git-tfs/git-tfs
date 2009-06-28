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
    //[Description("init [options] tfs-url repository-path [git-repository]")]
    public class Fetch : GitTfsCommand
    {
        public int Run(IEnumerable<string> args)
        {
        }
    }
}
