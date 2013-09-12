using System.IO;
using Rhino.Mocks;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class GitTfsRemoteTests
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

        private GitTfsRemote BuildRemote(string repository, string url = "", string[] legacyUrls = null)
        {
            if (legacyUrls == null)
                legacyUrls = new string[0];
            var info = new RemoteInfo
            {
                Id = "test",
                Url = url,
                Repository = repository,
                Aliases = legacyUrls,
            };
            var mocks = new RhinoAutoMocker<GitTfsRemote>();
            mocks.Inject<TextWriter>(new StringWriter());
            mocks.Inject<RemoteInfo>(info);
            mocks.Inject<ITfsHelper>(MockRepository.GenerateStub<ITfsHelper>()); // GitTfsRemote backs the TfsUrl with this.
            return mocks.ClassUnderTest;
        }
    }
}