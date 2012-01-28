using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NDesk.Options;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("shelve-list")]
    [Description("shelve-list -u <shelve-owner-name> [options]")]
    [RequiresValidGitRepository]
    public class ShelveList : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly Globals _globals;

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('s')]
        [LongOptionName("sort")]
        [UseNameAsLongOption(false)]
        [Description("How to sort resulting shelves. Possible values are: date, owner, name, comment.")]
        public string SortBy { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('f')]
        [LongOptionName("full")]
        [UseNameAsLongOption(false)]
        [Description("Use detailed format.")]
        public bool FullFormat { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('u')]
        [LongOptionName("user")]
        [UseNameAsLongOption(false)]
        [Description("Shelveset owner ('all' means all users)")]
        public string Owner { get; set; }

        public OptionSet OptionSet
        {
            get { return new OptionSet(); }
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(); }
        }

        public ShelveList(TextWriter stdout, Globals globals)
        {
            _stdout = stdout;
            _globals = globals;
        }

        public int Run()
        {
            var remote = _globals.Repository.ReadAllTfsRemotes().First();
            return remote.Tfs.ListShelvesets(this, remote);
        }
    }
}
