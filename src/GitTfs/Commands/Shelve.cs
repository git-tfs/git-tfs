using System.ComponentModel;
using NDesk.Options;
using GitTfs.Core;
using GitTfs.Util;
using StructureMap;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("shelve")]
    [Description("shelve [options] shelveset-name [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Shelve : GitTfsCommand
    {
        private readonly CheckinOptions _checkinOptions;
        private readonly CheckinOptionsFactory _checkinOptionsFactory;
        private readonly TfsWriter _writer;
        private readonly Globals _globals;

        private bool EvaluateCheckinPolicies { get; set; }

        public Shelve(CheckinOptions checkinOptions, TfsWriter writer, Globals globals)
        {
            _globals = globals;
            _checkinOptions = checkinOptions;
            _checkinOptionsFactory = new CheckinOptionsFactory(_globals);
            _writer = writer;
        }

        public OptionSet OptionSet => new OptionSet
                {
                    { "p|evaluate-policies", "Evaluate checkin policies (default: false)",
                        v => EvaluateCheckinPolicies = v != null },
                    { "f|force", "Force a shelve, and overwrite an existing shelveset",
                        v => { _checkinOptions.Force = true; } },
                }.Merge(_checkinOptions.OptionSet);

        public int Run(string shelvesetName) => Run(shelvesetName, "HEAD");

        public int Run(string shelvesetName, string refToShelve) => _writer.Write(refToShelve, (changeset, referenceToShelve) =>
                                                                             {
                                                                                 if (!_checkinOptions.Force && changeset.Remote.HasShelveset(shelvesetName))
                                                                                 {
                                                                                     Trace.TraceInformation("Shelveset \"" + shelvesetName + "\" already exists. Use -f to replace it.");
                                                                                     return GitTfsExitCodes.ForceRequired;
                                                                                 }

                                                                                 var commit = _globals.Repository.GetCommit(refToShelve);
                                                                                 var message = commit != null // this is only null in the unit tests
                                                                                     ? BuildCommitMessage(commit, !_checkinOptions.NoGenerateCheckinComment,
                                                                                         changeset.Remote.MaxCommitHash)
                                                                                     : string.Empty;

                                                                                 var shelveSpecificCheckinOptions = _checkinOptionsFactory.BuildShelveSetSpecificCheckinOptions(_checkinOptions, message);

                                                                                 changeset.Remote.Shelve(shelvesetName, referenceToShelve, changeset, shelveSpecificCheckinOptions, EvaluateCheckinPolicies);
                                                                                 return GitTfsExitCodes.OK;
                                                                             });

        public string BuildCommitMessage(GitCommit commit, bool generateCheckinComment, string latest) => generateCheckinComment
                               ? _globals.Repository.GetCommitMessage(commit.Sha, latest)
                               : _globals.Repository.GetCommit(commit.Sha).Message;
    }
}
