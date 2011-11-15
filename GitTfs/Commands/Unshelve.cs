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
    [Description("unshelve -u <shelve-owner-name> <shelve-name> <git-branch-name>")]
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
        [Description("Shelveset owner ('all' means all users)")]
        public string Owner { get; set; }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(); }
        }

        public int Run(IList<string> args)
        {
            // TODO -- let the remote be specified on the command line.
            var remote = _globals.Repository.ReadAllTfsRemotes().First();
            return remote.Tfs.Unshelve(this, remote, args);
        }
    }
}
