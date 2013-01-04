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
            var mocker = new RhinoAutoMocker<GitTfsRemote>();
            mocker.Inject<TextWriter>(new StringWriter());
            var helper = MockRepository.GenerateStub<ITfsHelper>();
            helper.Url = "http://testvcs:8080/tfs/test";
            helper.LegacyUrls = new string[0];
            mocker.Inject(helper);
            mocker.ClassUnderTest.TfsRepositoryPath = "test";

            bool matches = mocker.ClassUnderTest.MatchesUrlAndRepositoryPath("http://testvcs:8080/tfs/Test", "test");

            Assert.Equal(true, matches);
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_false_if_no_match_for_tfs_url()
        {
            var mocker = new RhinoAutoMocker<GitTfsRemote>();
            mocker.Inject<TextWriter>(new StringWriter());
            var helper = MockRepository.GenerateStub<ITfsHelper>();
            helper.Url = "http://testvcs:8080/tfs/test";
            helper.LegacyUrls = new string[0];
            mocker.Inject(helper);
            mocker.ClassUnderTest.TfsRepositoryPath = "test";

            bool matches = mocker.ClassUnderTest.MatchesUrlAndRepositoryPath("http://adifferenturl:8080/tfs/Test", "test");

            Assert.Equal(false, matches);
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_case_insensitive_for_legacy_urls()
        {
            var mocker = new RhinoAutoMocker<GitTfsRemote>();
            mocker.Inject<TextWriter>(new StringWriter());
            var helper = MockRepository.GenerateStub<ITfsHelper>();
            helper.Url = "";
            helper.LegacyUrls = new[] { "http://testvcs:8080/tfs/test", "AnotherUrlThatDoesntMatch" };
            mocker.Inject(helper);
            mocker.ClassUnderTest.TfsRepositoryPath = "test";

            bool matches = mocker.ClassUnderTest.MatchesUrlAndRepositoryPath("http://testvcs:8080/tfs/Test", "test");

            Assert.Equal(true, matches);
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_case_insensitive_for_tfs_repository_path()
        {
            var mocker = new RhinoAutoMocker<GitTfsRemote>();
            mocker.Inject<TextWriter>(new StringWriter());
            var helper = MockRepository.GenerateStub<ITfsHelper>();
            helper.Url = "test";
            helper.LegacyUrls = new string[0];
            mocker.Inject(helper);
            mocker.ClassUnderTest.TfsRepositoryPath = "$/Test";

            bool matches = mocker.ClassUnderTest.MatchesUrlAndRepositoryPath("test", "$/test");

            Assert.Equal(true, matches);
        }

        [Fact]
        public void MatchesUrlAndRepositoryPath_should_be_false_if_no_match_for_tfs_repository_path()
        {
            var mocker = new RhinoAutoMocker<GitTfsRemote>();
            mocker.Inject<TextWriter>(new StringWriter());
            var helper = MockRepository.GenerateStub<ITfsHelper>();
            helper.Url = "test";
            helper.LegacyUrls = new string[0];
            mocker.Inject(helper);
            mocker.ClassUnderTest.TfsRepositoryPath = "$/Test";

            bool matches = mocker.ClassUnderTest.MatchesUrlAndRepositoryPath("test", "$/shouldnotmatch");

            Assert.Equal(false, matches);
        }
    }
}