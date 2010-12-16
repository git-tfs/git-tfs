using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    public abstract class BaseTfsCommit : GitTfsCommand
    {
        private readonly Globals _globals;
        protected readonly TextWriter Stdout;
        protected readonly CheckinOptions CheckinOptions;

        protected BaseTfsCommit(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
        {
            _globals = globals;
            Stdout = stdout;
            CheckinOptions = checkinOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(CheckinOptions); }
        }

        public int Run(IList<string> args)
        {
            if (ShouldShowHelp(args.Count))
                return Help.ShowHelpForInvalidArguments(this);

            var refToShelve = GetRefToShelve(args);
            var tfsParents = _globals.Repository.GetParentTfsCommits(refToShelve);
            if (_globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == _globals.UserSpecifiedRemoteId);
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    return ExecuteCommit(changeset, refToShelve, args);
                case 0:
                    Stdout.WriteLine("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    Stdout.WriteLine("More than one parent found! Use -i to choose the correct parent from: ");
                    foreach (var parent in tfsParents)
                    {
                        Stdout.WriteLine("  " + parent.Remote.Id);
                    }
                    return GitTfsExitCodes.InvalidArguments;
            }
        }

        protected abstract bool ShouldShowHelp(int argCount);
        
        protected virtual string GetRefToShelve(IList<string> args)
        {
            return "HEAD";
        }

        protected virtual int ExecuteCommit(TfsChangesetInfo changeset, string refToShelve, IList<string> args)
        {
            return GitTfsExitCodes.OK;
        }
    }
}
