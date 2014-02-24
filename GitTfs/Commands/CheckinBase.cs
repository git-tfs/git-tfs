using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NDesk.Options;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    public abstract class CheckinBase : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        protected readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        protected CheckinBase(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
        {
            _stdout = stdout;
            _checkinOptions = checkinOptions;
            _writer = writer;
        }

        public OptionSet OptionSet
        {
            get { return _checkinOptions.OptionSet; }
        }

        public int Run()
        {
            return Run("HEAD");
        }

        public int Run(string refToCheckin)
        {
            return _writer.Write(refToCheckin, PerformCheckin);
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
                parentChangeset.Remote.FetchWithMerge(newChangesetId, false, refToCheckin);

                if (refToCheckin == "HEAD")
                    parentChangeset.Remote.Repository.Merge(parentChangeset.Remote.MaxCommitHash);
            }

            Trace.WriteLine("Cleaning...");
            parentChangeset.Remote.CleanupWorkspaceDirectory();

            return GitTfsExitCodes.OK;
        }

        protected abstract long DoCheckin(TfsChangesetInfo changeset, string refToCheckin);
    }
}
