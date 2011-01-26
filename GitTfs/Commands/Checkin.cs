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
        private readonly IHelpHelper _help;

        public Checkin(Globals globals, TextWriter stdout, CheckinOptions checkinOptions, IHelpHelper help)
        {
            _globals = globals;
            _help = help;
            _stdout = stdout;
            _checkinOptions = checkinOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(_checkinOptions); }
        }

        // TODO: DRY up, w.r.t. with Shelve.
        public int Run()
        {
            return Run("HEAD");
        }

        public int Run(string refToCheckin)
        {
            var tfsParents = _globals.Repository.GetParentTfsCommits(refToCheckin);
            if (_globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == _globals.UserSpecifiedRemoteId);
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    var newChangeset = changeset.Remote.Checkin(refToCheckin, changeset);
                    _stdout.WriteLine("TFS Changeset #" + newChangeset + " was created. Marking it as a merge commit...");
                    changeset.Remote.Fetch(new Dictionary<long, string>{{newChangeset, refToCheckin}});
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
