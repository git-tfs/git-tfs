using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("rcheckin")]
    [RequiresValidGitRepository]
    public class Rcheckin : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        public Rcheckin(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
        {
            _stdout = stdout;
            _checkinOptions = checkinOptions;
            _writer = writer;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(_checkinOptions); }
        }

        // uses rebase and works only with HEAD
        public int Run()
        {
            return _writer.Write("HEAD", PerformRCheckin);
        }

        private int PerformRCheckin(TfsChangesetInfo parentChangeset)
        {
            var tfsRemote = parentChangeset.Remote;
            var repo = tfsRemote.Repository;
            if (repo.WorkingCopyHasUnstagedOrUncommitedChanges)
            {
                throw new GitTfsException("error: You have local changes; rebase-workflow checkin only possible with clean working directory.")
                    .WithRecommendation("Try 'git stash' to stash your local changes and checkin again.");
            }

            // get latest changes from TFS to minimize possibility of late conflict
            _stdout.WriteLine("Fetching changes from TFS to minimize possibility of late conflict...");
            parentChangeset.Remote.Fetch();
            if (parentChangeset.ChangesetId != parentChangeset.Remote.MaxChangesetId)
            {
                throw new GitTfsException("error: New TFS changesets were found.")
                    .WithRecommendation("Try to rebase HEAD onto latest TFS checkin and repeat rcheckin or alternatively checkin s");
            }

            string revList = null;
            string tfsLatest = parentChangeset.Remote.MaxCommitHash;

            // we could rcheckin only if tfsLatest changeset is a parent of refToRCheckin
            // so that we could rebase after each single checkin without conflicts
            repo.CommandOutputPipe(output => revList = output.ReadToEnd(), "rev-list", tfsLatest, "^HEAD");
            if (revList != "")
                throw new GitTfsException("error: latest TFS commit should be parent of commits being checked in");

            while (true)
            {
                // determine first descendant of tfsLatest
                repo.CommandOutputPipe(output => revList = output.ReadLine(), "rev-list", "--abbrev-commit", "--parents", "--ancestry-path", "--first-parent", "--reverse", tfsLatest + "..HEAD");
                if (String.IsNullOrEmpty(revList))
                {
                    _stdout.WriteLine("No more to rcheckin.");
                    return GitTfsExitCodes.OK;
                }

                string[] strs = revList.Split(' ');
                string target = strs[0];
                string[] gitParents = strs.AsEnumerable().Skip(1).Where(hash => hash != tfsLatest).ToArray();

                string commitMessage = repo.GetCommitMessage(target, tfsLatest).Trim(' ', '\r', '\n');
                _stdout.WriteLine("Starting checkin of {0} '{1}'", target, commitMessage);
                _checkinOptions.CheckinComment = commitMessage;
                long newChangesetId = tfsRemote.Checkin(target, parentChangeset);
                tfsRemote.FetchWithMerge(newChangesetId, gitParents);
                if (tfsRemote.MaxChangesetId != newChangesetId)
                    throw new GitTfsException("error: New TFS changesets were found. Rcheckin was not finished.");
                
                tfsLatest = tfsRemote.MaxCommitHash;
                parentChangeset = new TfsChangesetInfo {ChangesetId = newChangesetId, GitCommit = tfsLatest, Remote = tfsRemote};
                _stdout.WriteLine("Done with {0}, rebasing tail onto new TFS-commit...", target);

                repo.CommandNoisy("rebase", "--preserve-merges", "--onto", tfsLatest, target);
                _stdout.WriteLine("Rebase done successfully.");
            }
        }
    }
}