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
    [Description("unshelve [options] shelve-name destination-branch")]
    [RequiresValidGitRepository]
    public class Unshelve : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly TextWriter _stdout;

        public Unshelve(Globals globals, TextWriter stdout)
        {
            _globals = globals;
            _stdout = stdout;
            Branch = _globals.RemoteId;
        }

        public string Owner { get; set; }
        public string Branch { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "u|user=", "Shelveset owner (default: current user)\nUse 'all' to search all shelvesets.",
                        v => Owner = v },
                    { "b|branch=", "GIT Branch for Shelfset(default: default)", 
                        v => Branch = v },                
                };
            }
        }

        public int Run(string shelvesetName, string destinationBranch)
        {
            var remote = _globals.Repository.ReadTfsRemote(Branch);
            remote.Unshelve(Owner, shelvesetName, destinationBranch);
            _stdout.WriteLine("Created branch " + destinationBranch + " from shelveset \"" + shelvesetName + "\".");
            return GitTfsExitCodes.OK;
        }
    }
}
