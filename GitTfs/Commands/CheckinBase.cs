using System;
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
            return _writer.Write(refToCheckin, changeset => PerformCheckin(changeset, refToCheckin));
        }

        private int PerformCheckin(TfsChangesetInfo parentChangeset, string refToCheckin)
        {
            var newChangesetId = DoCheckin(parentChangeset, refToCheckin);

            if (_checkinOptions.NoMerge)
            {
                _stdout.WriteLine("TFS Changeset #" + newChangesetId + " was created.");
                parentChangeset.Remote.Fetch();
            }
            else
            {
                _stdout.WriteLine("TFS Changeset #" + newChangesetId + " was created. Marking it as a merge commit...");
                parentChangeset.Remote.FetchWithMerge(newChangesetId, refToCheckin);

                if (refToCheckin == "HEAD")
                    parentChangeset.Remote.Repository.CommandNoisy("merge", parentChangeset.Remote.MaxCommitHash);
            }
            return GitTfsExitCodes.OK;
        }

        protected abstract long DoCheckin(TfsChangesetInfo changeset, string refToCheckin);
    }
}