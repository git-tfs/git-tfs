using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.ServiceModel.Syndication;

using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using GitTfs.Util;

using Moq;
using StructureMap.AutoMocking;
using Xunit;

namespace GitTfs.Test.Core
{
    public class GitTfsRemoteTests : BaseTest
    {
        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_case_insensitive_for_tfs_url()
        {
            var remote = BuildRemote(url: "http://testvcs:8080/tfs/test", repository: "test");
            Assert.True(remote.MatchesUrlAndRepositoryPath("http://testvcs:8080/tfs/Test", "test"));
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_false_if_no_match_for_tfs_url()
        {
            var remote = BuildRemote(url: "http://testvcs:8080/tfs/test", repository: "test");
            Assert.False(remote.MatchesUrlAndRepositoryPath("http://adifferenturl:8080/tfs/Test", "test"));
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_case_insensitive_for_legacy_urls()
        {
            var remote = BuildRemote(legacyUrls: new[] { "http://testvcs:8080/tfs/test", "AnotherUrlThatDoesntMatch" }, repository: "test");
            Assert.True(remote.MatchesUrlAndRepositoryPath("http://testvcs:8080/tfs/Test", "test"));
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_case_insensitive_for_tfs_repository_path()
        {
            var remote = BuildRemote(url: "test", repository: "$/Test");
            Assert.True(remote.MatchesUrlAndRepositoryPath("test", "$/test"));
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_false_if_no_match_for_tfs_repository_path()
        {
            var remote = BuildRemote(url: "test", repository: "$/Test");
            Assert.False(remote.MatchesUrlAndRepositoryPath("test", "$/shouldnotmatch"));
        }

        private GitTfsRemote BuildRemote(string repository, string url = "", string[] legacyUrls = null, string id = "test")
        {
            var info = new RemoteInfo
            {
                Id = id,
                Url = url,
                Repository = repository,
                Aliases = legacyUrls ?? new string[0],
            };
            var mocks = new MoqAutoMocker<GitTfsRemote>();
            mocks.Inject(info);
            var mockTfsHelper = new Mock<ITfsHelper>();
            mockTfsHelper.SetupAllProperties();
            mocks.Inject(mockTfsHelper.Object); // GitTfsRemote backs the TfsUrl with this.
            return mocks.ClassUnderTest;
        }

        [Fact]
        public void GivenTheTfsPathsInTheBranchFolder_WhenGettingPathInGitRepo_ThenShouldGetRelativePaths()
        {
            var remote = BuildRemote(url: "test", repository: "$/Project/MyBranch_other");
            Assert.Equal("", remote.GetPathInGitRepo("$/Project/MyBranch_other"));
            Assert.Equal("file.txt", remote.GetPathInGitRepo("$/Project/MyBranch_other/file.txt"));
        }

        [Fact]
        public void GivenTheTfsPathsInAnotherBranchFolder_WhenGettingPathInGitRepo_ThenShouldGetNothing()
        {
            var remote = BuildRemote(url: "test", repository: "$/Project/MyBranch");
            Assert.Null(remote.GetPathInGitRepo("$/Project/MyBranch_other"));
            Assert.Null(remote.GetPathInGitRepo("$/Project/MyBranch_other/file.txt"));
        }

        [Fact]
        public void GivenTheTfsPathsAreInOneOfTheSubRemotes_WhenGettingPathInGitRepoInSubtree_ThenShouldGetRelativePathes()
        {
            var subtreeRemote = BuildSubTreeOwnerRemote(new List<IGitTfsRemote>
                {
                    BuildRemote(url: "MyBranch", repository: "$/Project/MyBranch", id:"test_subtree/MyBranch"),
                    BuildRemote(url: "MyBranch_other", repository: "$/Project/MyBranch_other", id:"test_subtree/MyBranch_other"),
                });
            Assert.Equal("MyBranch_other/", subtreeRemote.GetPathInGitRepo("$/Project/MyBranch_other"));
            Assert.Equal("MyBranch_other/file.txt", subtreeRemote.GetPathInGitRepo("$/Project/MyBranch_other/file.txt"));
        }

        [Fact]
        public void GivenTheTfsPathsAreNotInOneOfTheSubRemotes_WhenGettingPathInGitRepoInSubtree_ThenShouldGetNothing()
        {
            var subtreeRemote = BuildSubTreeOwnerRemote(new List<IGitTfsRemote>
                {
                    BuildRemote(url: "MyBranch", repository: "$/Project/MyBranch", id:"test_subtree/MyBranch"),
                });
            Assert.Null(subtreeRemote.GetPathInGitRepo("$/Project/MyBranch_other"));
            Assert.Null(subtreeRemote.GetPathInGitRepo("$/Project/MyBranch_other/file.txt"));
        }

        [Fact]
        public void GivenAChangeset_ShouldNotSkipNextChangesets()
        {
            var mockGit = SetupGit();
            var changeset1 = new Mock<ITfsChangeset>();
            var changeset2 = new Mock<ITfsChangeset>();
            var changeset3 = new Mock<ITfsChangeset>();

            changeset1.Setup(o => o.Summary).Returns(new TfsChangesetInfo());
            changeset1.Setup(o => o.Apply(It.IsAny<string>(), It.IsAny<IGitTreeModifier> (), It.IsAny<ITfsWorkspace> (), It.IsAny<IDictionary<string, GitObject>>() ,It.IsAny<Action <Exception>>() )).Returns(new LogEntry());
            changeset1.Setup(o => o.IsRenameChangeset).Returns(true);

            changeset2.Setup(o => o.Summary).Returns(new TfsChangesetInfo());
            changeset2.Setup(o => o.Apply(It.IsAny<string>(), It.IsAny<IGitTreeModifier> (), It.IsAny<ITfsWorkspace> (), It.IsAny<IDictionary<string, GitObject>>() ,It.IsAny<Action <Exception>>() )).Returns(new LogEntry());

            changeset3.Setup(o => o.Summary).Returns(new TfsChangesetInfo() { ChangesetId = 2000 });
            changeset3.Setup(o => o.Apply(It.IsAny<string>(), It.IsAny<IGitTreeModifier>(), It.IsAny<ITfsWorkspace>(), It.IsAny<IDictionary<string, GitObject>>(), It.IsAny<Action<Exception>>())).Returns(new LogEntry());

            var changeSetResult = new List<ITfsChangeset>
            {
                changeset1.Object,
                changeset2.Object,
                changeset3.Object,
            };
            var mockWorkspace = new Mock<ITfsWorkspace>();

            var fakeTfs = new FakeTfsHelper(changeSetResult, mockWorkspace.Object);
            var globals = new Globals();
            globals.Repository = mockGit;
            globals.GitDir = "dir";

            var loader = new ConfigPropertyLoader(globals);
            var config = new ConfigProperties(loader);
            var remote = new GitTfsRemote(new RemoteInfo() { Id= "abc", Repository = "repo"}, mockGit, new RemoteOptions(), globals, fakeTfs, config);

            mockWorkspace.Setup(o => o.Remote).Returns(remote);

            var result = remote.Fetch();

            Assert.Equal(3, result.NewChangesetCount);
        }

        private IGitRepository SetupGit()
        {
            var mockTreeBuilder = new Mock<IGitTreeBuilder>();
            mockTreeBuilder.Setup(o => o.GetTree());
            var mockCommit = new Mock<IGitCommit>();
            var mockGit = new Mock<IGitRepository>();
            mockGit.Setup(o => o.GetConfig<int>(It.IsAny<string>())).Returns(122);
            mockGit.Setup(o => o.GetSubtrees(It.IsAny<IGitTfsRemote>())).Returns(new List<IGitTfsRemote>());
            mockGit.Setup(o => o.GetTreeBuilder(It.IsAny<string>())).Returns(mockTreeBuilder.Object);
            mockGit.Setup(o => o.Commit(It.IsAny<LogEntry>())).Returns(mockCommit.Object);
            return mockGit.Object;
        }

        private GitTfsRemote BuildSubTreeOwnerRemote(IEnumerable<IGitTfsRemote> remotes)
        {
            var info = new RemoteInfo
            {
                Id = "test",
                Url = null,
                Repository = null,
            };
            var mocks = new MoqAutoMocker<GitTfsRemote>();
            mocks.Inject(info);
            mocks.Inject(new Mock<ITfsHelper>()); // GitTfsRemote backs the TfsUrl with this.

            var mockGitRepository = new Mock<IGitRepository>();
            mockGitRepository.Setup(t => t.GetSubtrees(It.IsAny<IGitTfsRemote>())).Returns(remotes);

            mocks.Inject(new Globals() { Repository = mockGitRepository.Object });
            return mocks.ClassUnderTest;
        }
    }

    public class FakeTfsHelper : ITfsHelper
    {
        List<ITfsChangeset> _changesets;
        ITfsWorkspace _workspace;
        public FakeTfsHelper(List<ITfsChangeset> changesets, ITfsWorkspace workspace)
        {
            _changesets = changesets;
            _workspace = workspace;
        }

        public string TfsClientLibraryVersion => throw new NotImplementedException();

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public bool CanShowCheckinDialog => throw new NotImplementedException();

        public void CleanupWorkspaces(string workingDirectory)
        {
            throw new NotImplementedException();
        }

        public void CreateBranch(string sourcePath, string targetPath, int changesetId, string comment = null)
        {
            throw new NotImplementedException();
        }

        public ICheckinNote CreateCheckinNote(Dictionary<string, string> checkinNotes)
        {
            throw new NotImplementedException();
        }

        public IShelveset CreateShelveset(IWorkspace workspace, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public void CreateTfsRootBranch(string projectName, string mainBranch, string gitRepositoryPath, bool createTeamProjectFolder)
        {
            throw new NotImplementedException();
        }

        public void DeleteShelveset(IWorkspace workspace, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public void EnsureAuthenticated()
        {
            throw new NotImplementedException();
        }

        public int FindMergeChangesetParent(string path, int firstChangeset, GitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetAllTfsRootBranchesOrderedByCreation()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IBranchObject> GetBranches(bool getDeletedBranches = false)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetChangeset(int changesetId, IGitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public IChangeset GetChangeset(int changesetId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITfsChangeset> GetChangesets(string path, int startVersion, IGitTfsRemote remote, int lastVersion = -1, bool byLots = false)
        {
            return _changesets;
        }

        public IIdentity GetIdentity(string username)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TfsLabel> GetLabels(string tfsPathBranch, string nameFilter = null)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetLatestChangeset(IGitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public int GetLatestChangesetId(IGitTfsRemote remote)
        {
            return 122;
        }

        public IList<RootBranch> GetRootChangesetForBranch(string tfsPathBranchToCreate, int lastChangesetIdToCheck = -1, string tfsPathParentBranch = null)
        {
            throw new NotImplementedException();
        }

        public ITfsChangeset GetShelvesetData(IGitTfsRemote remote, string shelvesetOwner, string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorkItemCheckedInfo> GetWorkItemCheckedInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorkItemCheckinInfo> GetWorkItemInfos(IEnumerable<string> workItems, TfsWorkItemCheckinAction checkinAction)
        {
            throw new NotImplementedException();
        }

        public bool HasShelveset(string shelvesetName)
        {
            throw new NotImplementedException();
        }

        public bool IsExistingInTfs(string path)
        {
            throw new NotImplementedException();
        }

        public int ListShelvesets(ShelveList shelveList, IGitTfsRemote remote)
        {
            throw new NotImplementedException();
        }

        public int QueueGatedCheckinBuild(Uri value, string buildDefinitionName, string shelvesetName, string checkInTicket)
        {
            throw new NotImplementedException();
        }

        public int ShowCheckinDialog(IWorkspace workspace, IPendingChange[] pendingChanges, IEnumerable<IWorkItemCheckedInfo> checkedInfos, string checkinComment)
        {
            throw new NotImplementedException();
        }

        public void WithWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            action(_workspace);
        }

        public void WithWorkspace(string localDirectory, IGitTfsRemote remote, IEnumerable<Tuple<string, string>> mappings, TfsChangesetInfo versionToFetch, Action<ITfsWorkspace> action)
        {
            action(_workspace);
        }
    }
}