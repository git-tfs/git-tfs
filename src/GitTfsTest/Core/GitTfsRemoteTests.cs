using GitTfs.Core;
using GitTfs.Core.TfsInterop;

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
}