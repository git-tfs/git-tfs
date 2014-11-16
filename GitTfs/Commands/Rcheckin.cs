using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("rcheckin")]
    [RequiresValidGitRepository]
    public class Rcheckin : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly CommitSpecificCheckinOptionsFactory _checkinOptionsFactory;
        private readonly TfsWriter _writer;
        private readonly Globals _globals;
        private readonly AuthorsFile _authors;

        private bool Old { get; set; }
        private bool AutoRebase { get; set; }
        private bool ForceCheckin { get; set; }

        public Rcheckin(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer, Globals globals, AuthorsFile authors)
        {
            _stdout = stdout;
            _checkinOptions = checkinOptions;
            _checkinOptionsFactory = new CommitSpecificCheckinOptionsFactory(_stdout, globals, authors);
            _writer = writer;
            _globals = globals;
            _authors = authors;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                    {
                        { "q|no-rebase|quick", "'--quick' option is now deprecated because is became default one.",
                            v => { if( v != null ) _stdout.WriteLine("[deprecated] '--quick' option is now the default and no more needed. It will be removed in the next version..."); }
                        },
                        {"old", "Use the old process to rcheckin (slower)", v => Old = v != null},
                        {"a|autorebase", "Continue and rebase if new TFS changesets found", v => AutoRebase = v != null},
                        {"ignore-merge", "Force check in ignoring parent tfs branches in merge commits", v => ForceCheckin = v != null},
                    }.Merge(_checkinOptions.OptionSet);
            }
        }

        // uses rebase and works only with HEAD
        public int Run()
        {
            _globals.WarnOnGitVersion(_stdout);

            if (_globals.Repository.IsBare)
                throw new GitTfsException("error: you should specify the local branch to checkin for a bare repository.");

            return _writer.Write("HEAD", PerformRCheckin);
        }

        // uses rebase and works only with HEAD in a none bare repository
        public int Run(string localBranch)
        {
            _globals.WarnOnGitVersion(_stdout);

            if (!_globals.Repository.IsBare)
                throw new GitTfsException("error: This syntax with one parameter is only allowed in bare repository.");

            _authors.Parse(null, _globals.GitDir);

            return _writer.Write(GitRepository.ShortToLocalName(localBranch), PerformRCheckin);
        }

        private int PerformRCheckin(TfsChangesetInfo parentChangeset, string refToCheckin)
        {
            if (_globals.Repository.IsBare)
                AutoRebase = false;

            if (_globals.Repository.WorkingCopyHasUnstagedOrUncommitedChanges)
            {
                throw new GitTfsException("error: You have local changes; rebase-workflow checkin only possible with clean working directory.")
                    .WithRecommendation("Try 'git stash' to stash your local changes and checkin again.");
            }

            // get latest changes from TFS to minimize possibility of late conflict
            _stdout.WriteLine("Fetching changes from TFS to minimize possibility of late conflict...");
            parentChangeset.Remote.Fetch();
            if (parentChangeset.ChangesetId != parentChangeset.Remote.MaxChangesetId)
            {
                if (!Old && AutoRebase)
                {
                    _globals.Repository.CommandNoisy("rebase", "--preserve-merges", parentChangeset.Remote.RemoteRef);
                    parentChangeset = _globals.Repository.GetTfsCommit(parentChangeset.Remote.MaxCommitHash);
                }
                else
                {
                    if (_globals.Repository.IsBare)
                        _globals.Repository.UpdateRef(refToCheckin, parentChangeset.Remote.MaxCommitHash);

                    throw new GitTfsException("error: New TFS changesets were found.")
                        .WithRecommendation("Try to rebase HEAD onto latest TFS checkin and repeat rcheckin or alternatively checkin s");
                }
            }

            string tfsLatest = parentChangeset.Remote.MaxCommitHash;

            // we could rcheckin only if tfsLatest changeset is a parent of HEAD
            // so that we could rebase after each single checkin without conflicts
            if (!String.IsNullOrWhiteSpace(_globals.Repository.CommandOneline("rev-list", tfsLatest, "^" + refToCheckin)))
                throw new GitTfsException("error: latest TFS commit should be parent of commits being checked in");

            return (!Old || _globals.Repository.IsBare) ? _PerformRCheckinQuick(parentChangeset, refToCheckin) : _PerformRCheckin(parentChangeset, refToCheckin);
        }

        private int _PerformRCheckinQuick(TfsChangesetInfo parentChangeset, string refToCheckin)
        {
            var tfsRemote = parentChangeset.Remote;
            string tfsLatest = parentChangeset.Remote.MaxCommitHash;

            string[] revList = null;
            _globals.Repository.CommandOutputPipe(tr => revList = tr.ReadToEnd().Split('\n').Where(s => !String.IsNullOrWhiteSpace(s)).ToArray(),
                                   "rev-list", "--parents", "--ancestry-path", "--first-parent", "--reverse", tfsLatest + ".." + refToCheckin);

            string currentParent = tfsLatest;
            long newChangesetId = 0;

            var rc = new RCheckinCommit(_globals.Repository);

            foreach (string commitWithParents in revList)
            {
                rc.ExtractCommit(commitWithParents, currentParent);
                rc.BuildCommitMessage(!_checkinOptions.NoGenerateCheckinComment, currentParent);
                string target = rc.Sha;
                string tfsRepositoryPathOfMergedBranch = FindTfsRepositoryPathOfMergedBranch(tfsRemote, rc.Parents, target);

                var commitSpecificCheckinOptions = _checkinOptionsFactory.BuildCommitSpecificCheckinOptions(_checkinOptions, rc.Message, rc.Commit);

                _stdout.WriteLine("Starting checkin of {0} '{1}'", target.Substring(0, 8), commitSpecificCheckinOptions.CheckinComment);
                try
                {
                    newChangesetId = tfsRemote.Checkin(target, currentParent, parentChangeset, commitSpecificCheckinOptions, tfsRepositoryPathOfMergedBranch);
                    var fetchResult = tfsRemote.FetchWithMerge(newChangesetId, false, rc.Parents);
                    if (fetchResult.NewChangesetCount != 1)
                    {
                        var lastCommit = _globals.Repository.FindCommitHashByChangesetId(newChangesetId);
                        RebaseOnto(lastCommit, target);
                        if (AutoRebase)
                            tfsRemote.Repository.CommandNoisy("rebase", "--preserve-merges", tfsRemote.RemoteRef);
                        else
                            throw new GitTfsException("error: New TFS changesets were found. Rcheckin was not finished.");
                    }

                    currentParent = target;
                    parentChangeset = new TfsChangesetInfo {ChangesetId = newChangesetId, GitCommit = tfsRemote.MaxCommitHash, Remote = tfsRemote};
                    _stdout.WriteLine("Done with {0}.", target);
                }
                catch (Exception)
                {
                    if (newChangesetId != 0)
                    {
                        var lastCommit = _globals.Repository.FindCommitHashByChangesetId(newChangesetId);
                        RebaseOnto(lastCommit, currentParent);
                    }
                    throw;
                }
            }

            if (_globals.Repository.IsBare)
                _globals.Repository.UpdateRef(refToCheckin, tfsRemote.MaxCommitHash);
            else
                _globals.Repository.ResetHard(tfsRemote.MaxCommitHash);
            _stdout.WriteLine("No more to rcheckin.");

            Trace.WriteLine("Cleaning...");
            tfsRemote.CleanupWorkspaceDirectory();

            return GitTfsExitCodes.OK;
        }

        private int _PerformRCheckin(TfsChangesetInfo parentChangeset, string refToCheckin)
        {
            var tfsRemote = parentChangeset.Remote;
            string tfsLatest = parentChangeset.Remote.MaxCommitHash;

            var rc = new RCheckinCommit(_globals.Repository);

            while (true)
            {
                // determine first descendant of tfsLatest
                string revList = _globals.Repository.CommandOneline("rev-list", "--parents", "--ancestry-path", "--first-parent", "--reverse", tfsLatest + ".." + refToCheckin);
                if (String.IsNullOrWhiteSpace(revList))
                {
                    _stdout.WriteLine("No more to rcheckin.");

                    Trace.WriteLine("Cleaning...");
                    tfsRemote.CleanupWorkspaceDirectory();

                    return GitTfsExitCodes.OK;
                }

                rc.ExtractCommit(revList, tfsLatest);
                rc.BuildCommitMessage(!_checkinOptions.NoGenerateCheckinComment, tfsLatest);
                string target = rc.Sha;
                string tfsRepositoryPathOfMergedBranch = FindTfsRepositoryPathOfMergedBranch(tfsRemote, rc.Parents, target);

                var commitSpecificCheckinOptions = _checkinOptionsFactory.BuildCommitSpecificCheckinOptions(_checkinOptions, rc.Message, rc.Commit);

                _stdout.WriteLine("Starting checkin of {0} '{1}'", target.Substring(0, 8), commitSpecificCheckinOptions.CheckinComment);
                long newChangesetId = tfsRemote.Checkin(rc.Sha, parentChangeset, commitSpecificCheckinOptions, tfsRepositoryPathOfMergedBranch);
                tfsRemote.FetchWithMerge(newChangesetId, false, rc.Parents);
                if (tfsRemote.MaxChangesetId != newChangesetId)
                    throw new GitTfsException("error: New TFS changesets were found. Rcheckin was not finished.");

                tfsLatest = tfsRemote.MaxCommitHash;
                parentChangeset = new TfsChangesetInfo {ChangesetId = newChangesetId, GitCommit = tfsLatest, Remote = tfsRemote};
                _stdout.WriteLine("Done with {0}, rebasing tail onto new TFS-commit...", target);

                RebaseOnto(tfsLatest, target);
                _stdout.WriteLine("Rebase done successfully.");
            }
        }

        private struct RCheckinCommit
        {
            public GitCommit Commit { get; private set; }
            public string Sha { get; private set; }
            public string Message { get; private set; }
            public string[] Parents { get; private set; }

            private readonly IGitRepository _repo;

            public RCheckinCommit(IGitRepository repo)
                : this()
            {
                _repo = repo;
                Commit = null;
                Sha = null;
                Message = null;
                Parents = null;
            }

            public void ExtractCommit(string revList, string latest)
            {
                string[] commitShas = revList.Split(' ');
                Sha = commitShas[0];
                Parents = commitShas.AsEnumerable().Skip(1).Where(hash => hash != latest).ToArray();
                Commit = _repo.GetCommit(Sha);
            }

            public void BuildCommitMessage(bool generateCheckinComment, string latest)
            {
                Message = generateCheckinComment
                                   ? _repo.GetCommitMessage(Sha, latest)
                                   : _repo.GetCommit(Sha).Message;
            }
        }

        private string FindTfsRepositoryPathOfMergedBranch(IGitTfsRemote remoteToCheckin, string[] gitParents, string target)
        {
            if (gitParents.Length != 0)
            {
                _stdout.WriteLine("Working on the merge commit: " + target);
                if (gitParents.Length > 1)
                    _stdout.WriteLine("warning: only 1 parent is supported by TFS for a merge changeset. The other parents won't be materialized in the TFS merge!");
                foreach (var gitParent in gitParents)
                {
                    var tfsCommit = _globals.Repository.GetTfsCommit(gitParent);
                    if (tfsCommit != null)
                        return tfsCommit.Remote.TfsRepositoryPath;
                    var lastCheckinCommit = _globals.Repository.GetLastParentTfsCommits(gitParent).FirstOrDefault();
                    if (lastCheckinCommit != null)
                    {
                        if(!ForceCheckin && lastCheckinCommit.Remote.Id != remoteToCheckin.Id)
                            throw new GitTfsException("error: the merged branch '" + lastCheckinCommit.Remote.Id
                                + "' is a TFS tracked branch ("+lastCheckinCommit.Remote.TfsRepositoryPath
                                + ") with some commits not checked in.\nIn this case, the local merge won't be materialized as a merge in tfs...")
                                .WithRecommendation("check in all the commits of the tfs merged branch in TFS before trying to check in a merge commit",
                                "use --ignore-merge option to ignore merged TFS branch and check in commit as a normal changeset (not a merge).");
                    }
                    else
                    {
                        _stdout.WriteLine("warning: the parent " + gitParent + " does not belong to a TFS tracked branch (not checked in TFS) and will be ignored!");
                    }
                }
            }
            return null;
        }

        public void RebaseOnto(string newBaseCommit, string oldBaseCommit)
        {
            _globals.Repository.CommandNoisy("rebase", "--preserve-merges", "--onto", newBaseCommit, oldBaseCommit);
        }
    }
}
