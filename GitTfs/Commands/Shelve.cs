using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("shelve")]
    [Description("shelve [options] shelveset-name [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Shelve : BaseTfsCommit
    {
        [OptDef(OptValType.Flag)]
        [ShortOptionName('p')]
        [LongOptionName("evaluate-policies")]
        [UseNameAsLongOption(false)]
        [Description("Evaluate checkin policies")]
        public bool EvaluateCheckinPolicies { get; set; }

        public Shelve(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
            : base(globals, stdout, checkinOptions)
        {}

        protected override bool ShouldShowHelp(int argCount)
        {
            return argCount > 2;
        }

        protected override string GetRefToShelve(IList<string> args)
        {
            return args.Count > 1 ? args[1] : base.GetRefToShelve(args);
        }

        protected override int ExecuteCommit(TfsChangesetInfo changeset, string refToShelve, IList<string> args)
        {
            var shelvesetName = args[0];
            if (!CheckinOptions.Force && changeset.Remote.HasShelveset(shelvesetName))
            {
                Stdout.WriteLine("Shelveset \"" + shelvesetName + "\" already exists. Use -f to replace it.");
                return GitTfsExitCodes.ForceRequired;
            }
            changeset.Remote.Shelve(shelvesetName, refToShelve, changeset, EvaluateCheckinPolicies);
            return base.ExecuteCommit(changeset, refToShelve, args);
        }
    }
}
