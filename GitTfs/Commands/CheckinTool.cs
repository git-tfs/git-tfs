using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [PluggableWithAliases("checkintool", "ct")]
    [Description("checkintool [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class CheckinTool : GitTfsCommand
    {
        private readonly Globals globals;
        private readonly TextWriter stdout;
        private readonly CheckinOptions checkinOptions;
        private readonly IHelpHelper _help;

        public CheckinTool(Globals globals, TextWriter stdout, CheckinOptions checkinOptions, IHelpHelper help)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.checkinOptions = checkinOptions;
            _help = help;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(checkinOptions); }
        }

        public int Run(IList<string> args)
        {
            if (args.Count != 0 && args.Count != 1)
                return _help.ShowHelpForInvalidArguments(this);

            var refToShelve = args.Count > 0 ? args[0] : "HEAD";
            var tfsParents = globals.Repository.GetParentTfsCommits(refToShelve);
            
            if (globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == globals.UserSpecifiedRemoteId);
            
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    changeset.Remote.CheckinTool(refToShelve, changeset);
                    return GitTfsExitCodes.OK;
                case 0:
                    stdout.WriteLine("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    stdout.WriteLine("More than one parent found! Use -i to choose the correct parent from: ");
                    foreach (var parent in tfsParents)
                    {
                        stdout.WriteLine("  " + parent.Remote.Id);
                    }
                    return GitTfsExitCodes.InvalidArguments;
            }
        }
    }
}
