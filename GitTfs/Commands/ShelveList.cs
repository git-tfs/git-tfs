using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NDesk.Options;
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

        public string SortBy { get; set; }
        public bool FullFormat { get; set; }
        public string Owner { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "s|sort=", "How to sort shelvesets\ndate, owner, name, comment",
                        v => SortBy = v },
                    { "f|full", "Detailed output",
                        v => FullFormat = v != null },
                    { "u|user=", "Shelveset owner (default: current user)\nUse 'all' to get all shelvesets.",
                        v => Owner = v },
                };
            }
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
