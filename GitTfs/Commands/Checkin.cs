using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        [OptDef(OptValType.Flag)]
        [LongOptionName("pull")]
        [UseNameAsLongOption(false)]
        [Description("Merges the new TFS changeset to this branch, if checking in HEAD.")]
        public bool MergeBack { get; set; }

        public Checkin(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
        {
            _writer = writer;
            _stdout = stdout;
            _checkinOptions = checkinOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(_checkinOptions); }
        }

        public int Run()
        {
            MergeBack = true;
            return Run("HEAD");
        }

        public int Run(string refToCheckin)
        {
            if(refToCheckin != "HEAD" && MergeBack)
                throw new GitTfsException("Can't pull unless checking in HEAD!");
            return _writer.Write(refToCheckin, changeset =>
            {
                var newChangeset = changeset.Remote.Checkin(refToCheckin, changeset);
                _stdout.WriteLine("TFS Changeset #" + newChangeset + " was created. Marking it as a merge commit...");
                changeset.Remote.Fetch(new Dictionary<long, string> { { newChangeset, refToCheckin } });
                if(MergeBack)
                    changeset.Remote.Repository.CommandNoisy("merge", changeset.Remote.MaxCommitHash);
                return GitTfsExitCodes.OK;
            });
        }
    }
}
