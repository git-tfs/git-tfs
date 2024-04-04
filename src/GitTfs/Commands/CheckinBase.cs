using System.Diagnostics;
using NDesk.Options;
using GitTfs.Core;

namespace GitTfs.Commands
{
    public abstract class CheckinBase : GitTfsCommand
    {
        protected readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        protected CheckinBase(CheckinOptions checkinOptions, TfsWriter writer)
        {
            _checkinOptions = checkinOptions;
            _writer = writer;
        }

        public OptionSet OptionSet => _checkinOptions.OptionSet;

        public int Run() => Run("HEAD");

        public int Run(string refToCheckin) => _writer.Write(refToCheckin, PerformCheckin);

        private int PerformCheckin(TfsChangesetInfo parentChangeset, string refToCheckin)
        {
            var newChangesetId = DoCheckin(parentChangeset, refToCheckin);

            if (_checkinOptions.NoMerge)
            {
                Trace.TraceInformation($"TFS Changeset #{newChangesetId} was created.");
                parentChangeset.Remote.Fetch();
            }
            else
            {
                Trace.TraceInformation($"TFS Changeset #{newChangesetId} was created. Marking it as a merge commit...");
                parentChangeset.Remote.FetchWithMerge(newChangesetId, false, refToCheckin);

                if (refToCheckin == "HEAD")
                    parentChangeset.Remote.Repository.Merge(parentChangeset.Remote.MaxCommitHash);
            }

            Trace.WriteLine("Cleaning...");
            parentChangeset.Remote.CleanupWorkspaceDirectory();

            return GitTfsExitCodes.OK;
        }

        protected abstract int DoCheckin(TfsChangesetInfo changeset, string refToCheckin);
    }
}
