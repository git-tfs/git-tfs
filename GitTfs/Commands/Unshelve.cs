using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;

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

        public string Owner { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "u|user=", "Shelveset owner (default: current user)\nUse 'all' to search all shelvesets.",
                        v => Owner = v },
                };
            }
        }

        public int Run(IList<string> args)
        {
            var remote = _globals.Repository.ReadTfsRemote(_globals.RemoteId);
            return remote.Tfs.Unshelve(this, remote, args);
        }
    }
}
