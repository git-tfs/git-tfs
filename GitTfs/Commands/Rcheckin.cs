using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;
using System.Text.RegularExpressions;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("rcheckin")]
    [RequiresValidGitRepository]
    public class Rcheckin : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        private bool Quick { get; set; }

        public Rcheckin(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
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
                    { "no-rebase|quick", "omit rebases (faster)\nNote: this can lead to problems if someone checks something in while the command is running.",
                        v => Quick = v != null },
                }.Merge(_checkinOptions.OptionSet);
            }
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
            MatchCollection workitemMatches; 

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

            string tfsLatest = parentChangeset.Remote.MaxCommitHash;

            // we could rcheckin only if tfsLatest changeset is a parent of HEAD
            // so that we could rebase after each single checkin without conflicts
            if (!String.IsNullOrWhiteSpace(repo.CommandOneline("rev-list", tfsLatest, "^HEAD")))
                throw new GitTfsException("error: latest TFS commit should be parent of commits being checked in");

            if (Quick)
            {
                string[] revList = null;
                repo.CommandOutputPipe(tr => revList = tr.ReadToEnd().Split('\n').Where(s => !String.IsNullOrWhiteSpace(s)).ToArray(), 
                    "rev-list", "--parents", "--ancestry-path", "--first-parent", "--reverse", tfsLatest + "..HEAD");

                string currentParent = tfsLatest;
                foreach (string commitWithParents in revList)
                {
                    string[] strs = commitWithParents.Split(' ');
                    string target = strs[0];
                    string[] gitParents = strs.AsEnumerable().Skip(1).Where(hash => hash != currentParent).ToArray();

                    string commitMessage = repo.GetCommitMessage(target, currentParent).Trim(' ', '\r', '\n');
                    if ((workitemMatches = Sep.Git.Tfs.GitTfsConstants.TfsWorkItemRegex.Matches(commitMessage)).Count > 0)
                    {
                        foreach(Match match in workitemMatches){
                            switch(match.Groups["action"].Value){
                                case "associate": 
                                    _checkinOptions.WorkItemsToAssociate.Add(match.Groups["item_id"].Value);
                                    break;
                                case "resolve": 
                                    _checkinOptions.WorkItemsToResolve.Add(match.Groups["item_id"].Value);
                                    break;
                            }
                        }
                    }
                    _stdout.WriteLine("Starting checkin of {0} '{1}'", target.Substring(0, 8), commitMessage);
                    _checkinOptions.CheckinComment = commitMessage;
                    long newChangesetId = tfsRemote.Checkin(target, currentParent, parentChangeset);
                    tfsRemote.FetchWithMerge(newChangesetId, gitParents);
                    if (tfsRemote.MaxChangesetId != newChangesetId)
                        throw new GitTfsException("error: New TFS changesets were found. Rcheckin was not finished.");

                    currentParent = target;
                    parentChangeset = new TfsChangesetInfo { ChangesetId = newChangesetId, GitCommit = tfsRemote.MaxCommitHash, Remote = tfsRemote };
                    _stdout.WriteLine("Done with {0}.", target);
                }

                _stdout.WriteLine("No more to rcheckin.");
                return GitTfsExitCodes.OK;
            }
            else
            {
                while (true)
                {
                    // determine first descendant of tfsLatest
                    string revList = repo.CommandOneline("rev-list", "--parents", "--ancestry-path", "--first-parent", "--reverse", tfsLatest + "..HEAD");
                    if (String.IsNullOrWhiteSpace(revList))
                    {
                        _stdout.WriteLine("No more to rcheckin.");
                        return GitTfsExitCodes.OK;
                    }

                    string[] strs = revList.Split(' ');
                    string target = strs[0];
                    string[] gitParents = strs.AsEnumerable().Skip(1).Where(hash => hash != tfsLatest).ToArray();

                    string commitMessage = repo.GetCommitMessage(target, tfsLatest).Trim(' ', '\r', '\n');
                    _stdout.WriteLine("Starting checkin of {0} '{1}'", target.Substring(0, 8), commitMessage);
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
}
