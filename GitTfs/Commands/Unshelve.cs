using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("unshelve")]
    [Description("unshelve [options] (-l | shelveset-name [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Unshelve : GitTfsCommand
    {
        private readonly Globals _globals;

        public Unshelve(Globals globals)
        {
            _globals = globals;
        }
        [OptDef(OptValType.Flag)]
        [ShortOptionName('l')]
        [UseNameAsLongOption(false)]
        [Description("List available shelvesets")]
        public bool List { get; set; }

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
            // TODO -- let the remote be specified on the command line.
            var remote = _globals.Repository.ReadAllTfsRemotes().First();
            remote.Tfs.Unshelve(this, args);
            return GitTfsExitCodes.OK;
        }
    }
}
