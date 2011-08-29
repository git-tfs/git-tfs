using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;
namespace Sep.Git.Tfs.Commands
{
    [Pluggable("unshelve")]
    [Description("unshelve [options] (-l | shelveset-name destination-branch)")]
    [RequiresValidGitRepository]
    public class Unshelve : GitTfsCommand
    {
        private readonly Globals _globals;

        public Unshelve(Globals globals)
        {
            _globals = globals;
        }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('u')]
        [LongOptionName("user")]
        [UseNameAsLongOption(false)]
        [Description("Shelveset owner (default is the current user; 'all' means all users)")]
        public string Owner { get; set; }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(); }
        }

        public int Run(IList<string> args)
        {
            var remote = _globals.Repository.ReadTfsRemote(_globals.RemoteId);
            return remote.Tfs.Unshelve(this, remote, args);
        }
    }
}
