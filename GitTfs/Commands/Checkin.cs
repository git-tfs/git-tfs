using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkin")]
    [Description("checkin [options] [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Checkin : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;

        public Checkin(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
        {
            _globals = globals;
            _stdout = stdout;
            _checkinOptions = checkinOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(_checkinOptions); }
        }

        // TODO: DRY up, w.r.t. with Shelve.
        public int Run(IList<string> args)
        {
            if (args.Count != 0 && args.Count != 1)
                return Help.ShowHelpForInvalidArguments(this);
            var refToShelve = args.Count > 0 ? args[0] : "HEAD";
            var tfsParents = _globals.Repository.GetParentTfsCommits(refToShelve);
            if (_globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == _globals.UserSpecifiedRemoteId);
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    changeset.Remote.Checkin(refToShelve, changeset);
                    return GitTfsExitCodes.OK;
                case 0:
                    _stdout.WriteLine("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    _stdout.WriteLine("More than one parent found! Use -i to choose the correct parent from: ");
                    foreach (var parent in tfsParents)
                    {
                        _stdout.WriteLine("  " + parent.Remote.Id);
                    }
                    return GitTfsExitCodes.InvalidArguments;
            }
        }
    }
}
