using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("shelve")]
    [Description("shelve [options] shelveset-name [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Shelve : GitTfsCommand
    {
        private readonly Globals globals;
        private readonly TextWriter stdout;
        private readonly CheckinOptions checkinOptions;

        public Shelve(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.checkinOptions = checkinOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(checkinOptions); }
        }

        public int Run(IList<string> args)
        {
            if (args.Count != 1 && args.Count != 2)
                return Help.ShowHelpForInvalidArguments(this);
            var shelvesetName = args[0];
            var refToShelve = args.Count > 1 ? args[1] : "HEAD";
            var tfsParents = globals.Repository.GetParentTfsCommits(refToShelve);
            if (globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == globals.UserSpecifiedRemoteId);
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    if (!checkinOptions.Force && changeset.Remote.HasShelveset(shelvesetName))
                    {
                        stdout.WriteLine("Shelveset \"" + shelvesetName + "\" already exists. Use -f to replace it.");
                        return GitTfsExitCodes.ForceRequired;
                    }
                    changeset.Remote.Shelve(shelvesetName, refToShelve, changeset);
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
