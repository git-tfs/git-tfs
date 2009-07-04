namespace Sep.Git.Tfs.Commands
{
    public class GitTfsRemote
    {
        private RemoteOptions remoteOptions;

        public string Id { get; set; }
        public string TfsUrl { get; set; }
        public string TfsRepositoryPath { get; set; }
        public string IgnoreRegex { get; set; }
        public string Username { get; set; }
        public int MaxChangesetId { get; set; }
        public string MaxCommitHash { get; set; }
        public IGitRepository Repository { get; set; }
        public ITfsHelper Tfs { get; set; }

        private string Dir
        {
            get
            {
                return Ext.CombinePaths(Repository.GitDir, "tfs", Id);
            }
        }

        // This may need to be initialized like this:
        // git-svn.perl:5330: command_input_pipe(qw/update-index -z --index-info/)
        private string IndexFile
        {
            get
            {
                return Path.Combine(Dir, "index");
            }
        }

        public void Fetch()
        {
            var ignoreRegexPattern = remoteOptions.IgnoreRegex ?? IgnoreRegex;
            var ignoreRegex = ignoreRegexPattern == null ? null : new Regex(ignoreRegexPattern);
            foreach(var changeset in Tfs.GetChangesets(MaxChangesetId + 1))
            {
                AssertIndexClean(MaxCommitHash);
                var log = Apply(changeset);
                MaxCommitHash = Commit(log);
                MaxChangesetId = changeset.Id;
                DoGcIfNeeded();
            }
        }

        private void DoGcIfNeeded()
        {
            if(--globals.GcCountdown == 0)
            {
                globals.GcCountdown = globals.GcPeriod;
                Repository.CommandNoisy("gc", "--auto");
            }
        }

        private void AssertIndexClean(string treeish)
        {
            WithTemporaryIndex(() => {
                if(!File.Exists(IndexFile)) Repository.CommandNoisy("read-tree", treeish);
                var currentTree = Repository.CommandOneline("write-tree");
                var expectedCommitInfo = Repository.Command("cat-file", "commit", treeish);
                var expectedCommitTree = expectedCommitInfo.Match(new Regex("^tree (" + GitTfsConstants.Sha1 + ")")).Groups[1].Value;
                if(expectedCommitTree != currentTree)
                {
                    // warn "Index mismatch: $y != $x\nrereading $treeish\n";
                    File.Delete(IndexFile);
                    Repository.CommandNoisy("read-tree", treeish);
                    currentTree = Repository.CommandOneline("write-tree");
                    if(expectedCommitTree != currentTree)
                    {
                        throw new Exception("Unable to create a clean temporary index: trees (" + treeish + ") " + expectedCommitTree + " != " + currentTree);
                    }
                }
            });
        }

        private TBDLogEntry Apply(TfsChangeset changeset)
        {
            //TODO: see SVN::Git::Fetcher (3320) and Git::IndexInfo (5323)
        }

        private string Commit(TBDLogEntry logEntry)
        {
            WithCommitHeaderEnv(logEntry, () => {
                var tree = logEntry.Tree;
                if(tree == null)
                    WithTemporaryIndex(() => tree = Repository.CommandOneline("write-tree"));
                if(!tree.IsMatch(new Regex("^" + GitTfsConstants.Sha1 + "$"))
                    throw new "Tree is not a valid sha1: " + tree;
                var commitCommand = new List<string> { "commit-tree", tree };
                foreach(var parent in logEntry.CommitParents)
                {
                    commitCommand.Add("-p");
                    commitCommand.Add(parent);
                }
                // encode logEntry.Log according to 'git config --get i18n.commitencoding', if specified
                Repository.Open3(
                    stderr -> Console.Err,
                    stdin  <- logEntry.Log, metadata (git-tfs-id: ...),
                    stdout -> var commitInfo,
                    commitCommand.ToArray());
            });
            // TODO: StoreChangesetMetadata(commitInfo);
            return commitInfo.Sha1;
        }

        private void WithCommitHeaderEnv(TBDLogEntry logEntry, Action action)
        {
            WithTemporaryEnvironment(action, new Dictionary<string,string> {
                "GIT_AUTHOR_NAME" => logEntry.Name,
                "GIT_AUTHOR_EMAIL" => logEntry.Email,
                "GIT_AUTHOR_DATE" => logEntry.Date,
                "GIT_COMMITTER_DATE" => logEntry.Date,
                "GIT_COMMITTER_NAME" => logEntry.CommitterName ?? logEntry.Name,
                "GIT_COMMITTER_EMAIL" => logEntry.CommitterEmail ?? logEntry.Email
            });
        }

        private void WithTemporaryIndex(Action action)
        {
            WithTemporaryEnvironment(() => {
                Directory.CreateDirectory(Path.GetDirectoryName(IndexFile));
                action();
            }, new Dictionary<string,string> { "GIT_INDEX_FILE" => IndexFile });
        }

        private void WithTemporaryEnvironment(Action action, IDictionary<string, string> newEnvironment)
        {
            var oldEnvironment = new Dictionary<string, string>();
            PushEnvironment(newEnvironment, oldEnvironment);
            try
            {
                action();
            }
            finally
            {
                PushEnvironment(oldEnvironment);
            }
        }

        private void PushEnvironment(IDictionary<string, string> desiredEnvironment)
        {
            PushEnvironment(desiredEnvironment, new Dictionary<string, string>());
        }

        private void PushEnvironment(IDictionary<string, string> desiredEnvironment, IDictionary<string, string> oldEnvironment)
        {
            foreach(var key in desiredEnvironment.Keys)
            {
                oldEnvironment[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, desiredEnvironment[key]);
            }
        }
    }
}
