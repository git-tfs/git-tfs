using System.ComponentModel;
using System.IO;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("shelve")]
    [Description("shelve [options] shelveset-name [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Shelve : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        private bool EvaluateCheckinPolicies { get; set; }

        public Shelve(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
        {
            _stdout = stdout;
            _checkinOptions = checkinOptions;
            _writer = writer;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "p|evaluate-policies", "Evaluate checkin policies (default: false)",
                        v => EvaluateCheckinPolicies = v != null },
                    { "f|force", "Force a shelve, and overwrite an existing shelveset",
                        v => { this._checkinOptions.Force = true; } },
                }.Merge(_checkinOptions.OptionSet);
            }
        }

        public int Run(string shelvesetName)
        {
            return Run(shelvesetName, "HEAD");
        }

        public int Run(string shelvesetName, string refToShelve)
        {
            return _writer.Write(refToShelve, (changeset, referenceToShelve) =>
            {
                if (!_checkinOptions.Force && changeset.Remote.HasShelveset(shelvesetName))
                {
                    _stdout.WriteLine("Shelveset \"" + shelvesetName + "\" already exists. Use -f to replace it.");
                    return GitTfsExitCodes.ForceRequired;
                }
                changeset.Remote.Shelve(shelvesetName, referenceToShelve, changeset, EvaluateCheckinPolicies);
                return GitTfsExitCodes.OK;
            });
        }
    }
}
