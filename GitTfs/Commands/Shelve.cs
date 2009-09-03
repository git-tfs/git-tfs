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
            var remote = globals.Repository.ReadTfsRemote(globals.RemoteId);
            switch(args.Count)
            {
                case 1:
                    remote.Shelve(args[0], "HEAD");
                    break;
                case 2:
                    remote.Shelve(args[0], args[1]);
                    break;
                default:
                    return Help.ShowHelpForInvalidArguments(this);
            }
            return GitTfsExitCodes.OK;
        }
    }
}
