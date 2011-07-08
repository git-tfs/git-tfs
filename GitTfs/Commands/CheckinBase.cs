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
            if (_checkinOptions.RebaseWorkflow)
            {
                var repo = parentChangeset.Remote.Repository;
                if (repo.WorkingCopyHasUnstagedOrUncommitedChanges)
                {
                    throw new GitTfsException("error: You have local changes; rebase-workflow checkin only possible with clean working directory.")
                        .WithRecommendation("Try 'git stash' to stash your local changes and checkin again.");
                }

                var newChangesetId = DoCheckin(parentChangeset, refToCheckin);
                _stdout.WriteLine("TFS Changeset #" + newChangesetId + " was created");
                parentChangeset.Remote.Fetch();

                // if committed changeset is 'ultimate parent' to the HEAD, i.e. every parent of refToCheckin is also
                // parent of HEAD - then we could just rebase HEAD onto newly created changeset safely
                string revList = null;
                repo.CommandOutputPipe(output => revList = output.ReadToEnd(), "rev-list", "^HEAD", refToCheckin);
                bool ultimateParent = revList == "";
                if (!ultimateParent)
                    throw new GitTfsException("git tfs checkin -r can only TFS-checkin commits on the current branch, so that the rebase will work correctly.");

                _stdout.WriteLine("Rebasing onto committed changeset...");
                parentChangeset.Remote.Repository.CommandNoisy("rebase", "--onto", parentChangeset.Remote.MaxCommitHash, refToCheckin);
                return GitTfsExitCodes.OK;
            }
            else
            {
                var newChangesetId = DoCheckin(parentChangeset, refToCheckin);
                _stdout.WriteLine("TFS Changeset #" + newChangesetId + " was created. Marking it as a merge commit...");
                parentChangeset.Remote.Fetch(new Dictionary<long, string> { { newChangesetId, refToCheckin } });
                if (refToCheckin == "HEAD")
                    parentChangeset.Remote.Repository.CommandNoisy("merge", parentChangeset.Remote.MaxCommitHash);
                return GitTfsExitCodes.OK;
            }
        }

        protected abstract long DoCheckin(TfsChangesetInfo changeset, string refToCheckin);
    }
}