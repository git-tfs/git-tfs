using System.Collections.Generic;
using System.IO;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    public abstract class CheckinBase : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        protected CheckinBase(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
        {
            _stdout = stdout;
            _checkinOptions = checkinOptions;
            _writer = writer;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(_checkinOptions); }
        }

        public int Run()
        {
            return Run("HEAD");
        }

        public int Run(string refToCheckin)
        {
            return _writer.Write(refToCheckin, changeset =>
                                                   {
                                                       var newChangesetId = DoCheckin(changeset, refToCheckin);
                                                       _stdout.WriteLine("TFS Changeset #" + newChangesetId + " was created. Marking it as a merge commit...");
                                                       changeset.Remote.Fetch(new Dictionary<long, string> { { newChangesetId, refToCheckin } });
                                                       if (refToCheckin == "HEAD")
                                                           changeset.Remote.Repository.CommandNoisy("merge", changeset.Remote.MaxCommitHash);
                                                       return GitTfsExitCodes.OK;
                                                   });
        }

        protected abstract long DoCheckin(TfsChangesetInfo changeset, string refToCheckin);
    }
}