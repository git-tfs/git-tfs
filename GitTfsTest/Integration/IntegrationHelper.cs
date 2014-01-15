using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.VsFake;
using Xunit;
using Xunit.Sdk;

namespace Sep.Git.Tfs.Test.Integration
{
    class IntegrationHelper : IDisposable
    {
        #region manage the work directory

        string _workdir;
        private string Workdir
        {
            get
            {
                if(_workdir == null)
                {
                    _workdir = Path.GetTempFileName();
                    File.Delete(_workdir);
                    Directory.CreateDirectory(_workdir);
                }
                return _workdir;
            }
        }

        public void Dispose()
        {
            while (!_repositories.Empty())
            {
                var repo = _repositories.First();
                repo.Value.Dispose();
                _repositories.Remove(repo.Key);
            }
            if (_workdir != null)
            {
                try
                {
                    Directory.Delete(_workdir);
                    _workdir = null;
                }
                catch (Exception e)
                {
                }
            }
        }

        private Dictionary<string, Repository> _repositories = new Dictionary<string,Repository>();
        public Repository Repository(string path)
        {
            path = Path.Combine(Workdir, path);
            if (!_repositories.ContainsKey(path))
                _repositories.Add(path, new Repository(path));
            return _repositories[path];
        }

        #endregion

        #region set up a git repository

        public void SetConfig<T>(string repodir, string key, T value)
        {
            Repository(repodir).Config.Set(key, value);
        }

        public void SetupGitRepo(string path, Action<RepoBuilder> buildIt)
        {
            var repoPath = LibGit2Sharp.Repository.Init(Path.Combine(Workdir, path));
            using (var repo = new Repository(repoPath))
                buildIt(new RepoBuilder(repo));
        }

        public class RepoBuilder
        {
            private Repository _repo;

            public RepoBuilder(Repository repo)
            {
                _repo = repo;
            }

            public string Commit(string message)
            {
                File.WriteAllText(Path.Combine(_repo.Info.WorkingDirectory, "README.txt"), message);
                _repo.Index.Stage("README.txt");
                var committer = new Signature("Test User", "test@example.com", new DateTimeOffset(DateTime.Now));
                return _repo.Commit(message, committer, committer).Id.Sha;
            }
        }

        #endregion

        #region set up vsfake script

        public string FakeScript
        {
            get { return Path.Combine(Workdir, "_fakescript"); }
        }

        public void SetupFake(Action<FakeHistoryBuilder> scripter)
        {
            new Script().Tap(script => scripter(new FakeHistoryBuilder(script))).Save(FakeScript);
        }

        public class FakeHistoryBuilder
        {
            Script _script;
            public FakeHistoryBuilder(Script script)
            {
                _script = script;
            }

            public FakeChangesetBuilder Changeset(int changesetId, string message, DateTime checkinDate)
            {
                var changeset = new ScriptedChangeset
                {
                    Id = changesetId,
                    Comment = message,
                    CheckinDate = checkinDate,
                    IsBranchChangeset = false,
                    IsMergeChangeset = false,
                };
                _script.Changesets.Add(changeset);
                return new FakeChangesetBuilder(changeset);
            }

            public FakeChangesetBuilder BranchChangeset(int changesetId, string message, DateTime checkinDate, string fromBranch, string toBranch, int rootChangesetId)
            {
                var branchChangeset = new ScriptedChangeset
                {
                    Id = changesetId,
                    Comment = message,
                    CheckinDate = checkinDate,
                    IsBranchChangeset = true,
                    IsMergeChangeset = false,
                    BranchChangesetDatas = new BranchChangesetDatas
                    {
                        RootChangesetId = rootChangesetId,
                        BranchPath = toBranch,
                        ParentBranch = fromBranch
                    }
                };
                _script.Changesets.Add(branchChangeset);
                return new FakeChangesetBuilder(branchChangeset);
            }

            public FakeChangesetBuilder MergeChangeset(int changesetId, string message, DateTime checkinDate, string fromBranch, string intoBranch, int lastChangesetId)
            {
                var mergeChangeset = new ScriptedChangeset
                {
                    Id = changesetId,
                    Comment = message,
                    CheckinDate = checkinDate,
                    IsBranchChangeset = false,
                    IsMergeChangeset = true,
                    MergeChangesetDatas = new MergeChangesetDatas
                    {
                        BeforeMergeChangesetId = lastChangesetId,
                        BranchPath = fromBranch,
                        MergeIntoBranch = intoBranch
                    }
                };
                _script.Changesets.Add(mergeChangeset);
                return new FakeChangesetBuilder(mergeChangeset);
            }

            public void SetRootBranch(string rootBranchPath)
            {
                _script.RootBranches.Add(new ScriptedRootBranch(){BranchPath = rootBranchPath});
            }
        }

        public class FakeChangesetBuilder
        {
            ScriptedChangeset _changeset;

            public FakeChangesetBuilder(ScriptedChangeset changeset)
            {
                _changeset = changeset;
            }

            public FakeChangesetBuilder Change(TfsChangeType changeType, TfsItemType itemType, string tfsPath, string contents = null, int? itemId = null)
            {
                _changeset.Changes.Add(new ScriptedChange
                {
                    ChangeType = changeType,
                    ItemType = itemType,
                    RepositoryPath = tfsPath,
                    Content = contents,
                    ItemId = itemId
                });
                return this;
            }
        }

        #endregion

        #region run git-tfs

        public string TfsUrl { get { return "http://does/not/matter"; } }

        public int Run(params string[] args)
        {
            return RunIn(".", args);
        }

        public int RunIn(string workPath, params string[] args)
        {
            var origPwd = Environment.CurrentDirectory;
            var origClient = Environment.GetEnvironmentVariable("GIT_TFS_CLIENT");
            var origScript = Environment.GetEnvironmentVariable(Script.EnvVar);
            try
            {
                Environment.CurrentDirectory = Path.Combine(Workdir, workPath);
                Environment.SetEnvironmentVariable("GIT_TFS_CLIENT", "Fake");
                Environment.SetEnvironmentVariable(Script.EnvVar, FakeScript);
                Console.WriteLine(">> git tfs " + QuoteArgs(args));
                var argsWithDebug = new List<string>();
                argsWithDebug.Add("--debug");
                argsWithDebug.AddRange(args);
                return Program.MainCore(argsWithDebug.ToArray());
            }
            finally
            {
                Environment.SetEnvironmentVariable("GIT_TFS_CLIENT", origClient);
                Environment.SetEnvironmentVariable(Script.EnvVar, origScript);
                Environment.CurrentDirectory = origPwd;
            }
        }

        private string QuoteArgs(string[] args)
        {
            return string.Join(" ", args.Select(arg => QuoteArg(arg)).ToArray());
        }

        private string QuoteArg(string arg)
        {
            // This is not complete, but it is adequate for these tests.
            if (arg.Contains(' '))
                return '"' + arg + '"';
            return arg;
        }

        private string CurrentBuildPath
        {
            get
            {
                var path = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                return Path.GetDirectoryName(path);
            }
        }

        public void ChangeConfigSetting(string repodir, string key, string value)
        {
            var repo = new LibGit2Sharp.Repository(Path.Combine(Workdir, repodir));
            repo.Config.Set(key, value);
        }

        #endregion

        #region assertions

        public int GetCommitCount(string repodir)
        {
            return Repository(repodir).Commits.Count();
        }

        public void AssertGitRepo(string repodir)
        {
            var path = Path.Combine(Workdir, repodir);
            Assert.True(Directory.Exists(path), path + " should be a directory");
            Assert.True(Directory.Exists(Path.Combine(path, ".git")), path + " should have a .git dir inside of it");
        }

        public void AssertNoRef(string repodir, string gitref)
        {
            AssertEqual(null, RevParse(repodir, gitref), "Expected no ref " + gitref);
        }

        public void AssertRef(string repodir, string gitref, string expectedSha)
        {
            Assert.NotNull(expectedSha);
            AssertEqual(expectedSha, RevParse(repodir, gitref), "Expected " + gitref + " to be " + expectedSha);
        }

        private string RevParse(string repodir, string gitref)
        {
            var parsed = Repository(repodir).Lookup<Commit>(gitref);
            return parsed == null ? null : parsed.Sha;
        }

        private string ReadIfPresent(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }

        public void AssertEmptyWorkspace(string repodir)
        {
            var entries = new List<string>(Directory.GetFileSystemEntries(Path.Combine(Workdir, repodir)));
            entries = entries.Where(f => Path.GetFileName(f) != ".git").ToList();
            AssertEqual(new List<string>(), entries, "entries in " + repodir);
        }

        public void AssertCleanWorkspace(string repodir)
        {
            var status = Repository(repodir).Index.RetrieveStatus();
            AssertEqual(new List<string>(), status.Select(statusEntry => "" + statusEntry.State + ": " + statusEntry.FilePath).ToList(), "repo status");
        }

        public void AssertFileInWorkspace(string repodir, string file, string contents)
        {
            var path = Path.Combine(Workdir, repodir, file);
            var actual = File.ReadAllText(path, Encoding.UTF8);
            AssertEqual(contents, actual, "Contents of " + path);
        }

        public void AssertNoFileInWorkspace(string repodir, string file)
        {
            var path = Path.Combine(Workdir, repodir, file);
            Assert.False(File.Exists(path), "Expect " + file + " to be absent from " + repodir);
        }

        public void AssertTreeEntries(string repodir, string treeish, params string[] expectedPaths)
        {
            var obj = Repository(repodir).Lookup(treeish);
            var tree = obj is Tree ? (Tree)obj : ((Commit)obj).Tree;
            var entries = tree.Select(entry => entry.Path);
            Assert.Equal(expectedPaths.OrderBy(s => s), entries.OrderBy(s => s));
        }

        public void AssertCommitMessage(string repodir, string commitish, string message)
        {
            var commit = Repository(repodir).Lookup<Commit>(commitish);
            AssertEqual(message, commit.Message, "Commit message of " + commitish);
        }

        public void AssertConfig<T>(string repodir, string key, T expectedValue)
        {
            var config = Repository(repodir).Config.Get<T>(key);
            Assert.NotNull(config);
            Assert.Equal(expectedValue, config.Value);
        }

        private void AssertEqual<T>(T expected, T actual, string message)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (AssertActualExpectedException)
            {
                throw new AssertActualExpectedException(expected, actual, message);
            }
        }

        #endregion
    }
}
