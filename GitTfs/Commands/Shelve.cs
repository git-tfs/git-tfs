using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("shelve")]
    [Description("shelve [options] shelveset-name...")]
    [RequiresValidGitRepository]
    public class Shelve : GitTfsCommand
    {
        private readonly Globals globals;

        public Shelve(Globals globals)
        {
            this.globals = globals;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(); }
        }

        public int Run(IList<string> args)
        {
            if (args.Count != 1)
                return Help.ShowHelpForInvalidArguments(this);

            //var repo = globals.Repository;
            //var mostRecentChangeset = repo.WorkingHeadInfo("HEAD", true);
            return int.MinValue;
        }
    }
}
