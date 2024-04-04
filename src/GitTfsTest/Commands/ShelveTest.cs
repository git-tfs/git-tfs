using GitTfs.Commands;
using GitTfs.Core;
using Moq;
using StructureMap.AutoMocking;
using Xunit;

namespace GitTfs.Test.Commands
{
    public class ShelveTest : BaseTest
    {
        private readonly MoqAutoMocker<Shelve> mocks;
        private readonly Mock<IGitRepository> gitRepositoryMock;
        private readonly Mock<IGitTfsRemote> gitTfsRemoteMock;
        private readonly Mock<Globals> globalsMock;

        public ShelveTest()
        {
            mocks = new MoqAutoMocker<Shelve>();

            var gitRepository = mocks.Get<IGitRepository>();
            gitRepositoryMock = Mock.Get(gitRepository);

            var globals = mocks.Get<Globals>();
            globalsMock = Mock.Get(globals).SetupAllProperties();
            globals.Repository = gitRepository;

            var gitTfsRemote = mocks.Get<IGitTfsRemote>();
            gitTfsRemoteMock = Mock.Get(gitTfsRemote);
        }

        [Fact]
        public void ShouldFailWithLessThanOneParents()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits("my-head")).Returns(new TfsChangesetInfo[0]);

            Assert.NotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldFailWithMoreThanOneNonSubtreeParents()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";

            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits("my-head"))
                .Returns(new[] { ChangesetForRemote("good-choice"), ChangesetForRemote("another-good-choice") });

            Assert.NotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldFailWithMoreThanOneParentsWhenSpecifiedParentIsNotAParent()
        {
            var globals = mocks.Get<Globals>();
            globals.UserSpecifiedRemoteId = "wrong-choice";
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits("my-head"))
                .Returns(new[] { ChangesetForRemote("ok-choice"), ChangesetForRemote("good-choice") });

            Assert.NotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldSucceedWithMoreThanOneParentsWhenCorrectParentSpecified()
        {
            var globals = mocks.Get<Globals>();
            Mock.Get(globals).SetupAllProperties();
            globals.Repository = mocks.Get<IGitRepository>();
            globals.UserSpecifiedRemoteId = "good-choice";
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits("my-head"))
                .Returns(new[] { ChangesetForRemote("ok-choice"), ChangesetForRemote("good-choice") });

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldSucceedWithParentsFromSubtreeAndOwner()
        {
            globalsMock.Object.UserSpecifiedRemoteId = "good-choice";

            var subtree = ChangesetForRemote("good-choice_subtree/good");
            gitTfsRemoteMock.Setup(x => x.IsSubtree).Returns(true);
            gitRepositoryMock.Setup(x => x.ReadTfsRemote("good-choice")).Returns(gitTfsRemoteMock.Object);
            gitTfsRemoteMock.Setup(x => x.OwningRemoteId).Returns("good-choice");
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits("my-head"))
                .Returns(new[] { ChangesetForRemote("good-choice"), subtree });

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldSucceedForOneArgument()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            gitTfsRemoteMock.Setup(r => r.Id).Returns("default");
            gitRepositoryMock.Setup(x => x.ReadTfsRemote(It.IsAny<string>())).Returns(gitTfsRemoteMock.Object);
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits(It.IsAny<string>()))
                .Returns(new[] { new TfsChangesetInfo { Remote = gitTfsRemoteMock.Object } });

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care"));
        }

        [Fact]
        public void ShouldAskForCorrectParent()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            gitTfsRemoteMock.Setup(r => r.Id).Returns("default");
            //gitRepositoryMock.Setup(x => x.ReadTfsRemote(null)).IgnoreArguments().Returns(remote);
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits("commit_to_shelve"))
                .Returns(new[] { new TfsChangesetInfo { Remote = gitTfsRemoteMock.Object } });

            mocks.ClassUnderTest.Run("shelveset name", "commit_to_shelve");
        }

        [Fact]
        public void ShouldTellRemoteToShelve()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            gitTfsRemoteMock.Setup(r => r.Id).Returns("default");
            //gitRepositoryMock.Setup(x => x.ReadTfsRemote(null)).IgnoreArguments().Returns(remote);
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits(It.IsAny<string>()))
                .Returns(new[] { new TfsChangesetInfo { Remote = gitTfsRemoteMock.Object } });

            mocks.ClassUnderTest.Run("shelveset name");

            gitTfsRemoteMock.Verify(x => x.Shelve("shelveset name", "HEAD", It.IsAny<TfsChangesetInfo>(), It.IsAny<CheckinOptions>(), false), Times.Once);
        }

        [Fact]
        public void ShouldTellRemoteToShelveTreeish()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            gitTfsRemoteMock.Setup(r => r.Id).Returns("default");
            //gitRepositoryMock.Setup(x => x.ReadTfsRemote(null)).IgnoreArguments().Returns(remote);
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits(It.IsAny<string>()))
                .Returns(new[] { new TfsChangesetInfo { Remote = gitTfsRemoteMock.Object } });

            mocks.ClassUnderTest.Run("shelveset name", "treeish");

            gitTfsRemoteMock.Verify(x => x.Shelve("shelveset name", "treeish", It.IsAny<TfsChangesetInfo>(), It.IsAny<CheckinOptions>(), false), Times.Once);
        }

        [Fact]
        public void FailureCodeWhenShelvesetExists()
        {
            WireUpMockRemote();
            CreateShelveset("shelveset name");

            var exitCode = mocks.ClassUnderTest.Run("shelveset name", "treeish");
            Assert.Equal(GitTfsExitCodes.ForceRequired, exitCode);
        }

        [Fact]
        public void DoesNotTryToShelveIfShelvesetExists()
        {
            WireUpMockRemote();
            CreateShelveset("shelveset name");

            mocks.ClassUnderTest.Run("shelveset name", "treeish");

            gitTfsRemoteMock.Verify(
                x => x.Shelve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TfsChangesetInfo>(), It.IsAny<CheckinOptions>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void DoesNotStopIfForceIsSpecified()
        {
            mocks.Get<CheckinOptions>().Force = true;
            WireUpMockRemote();
            CreateShelveset("shelveset name");

            mocks.ClassUnderTest.Run("shelveset name", "treeish");

            gitTfsRemoteMock.Verify(
                x => x.Shelve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TfsChangesetInfo>(), It.IsAny<CheckinOptions>(), It.IsAny<bool>()), Times.Once);
        }

        private TfsChangesetInfo ChangesetForRemote(string remoteId)
        {
            gitTfsRemoteMock.Setup(x => x.Id).Returns(remoteId);
            return new TfsChangesetInfo { Remote = gitTfsRemoteMock.Object };
        }

        private void WireUpMockRemote()
        {
            mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            //var remote = mocks.Get<IGitTfsRemote>();
            gitTfsRemoteMock.Setup(x => x.Id).Returns("default");
            gitRepositoryMock.Setup(x => x.GetLastParentTfsCommits(It.IsAny<string>()))
                .Returns(new[] { new TfsChangesetInfo { Remote = gitTfsRemoteMock.Object } });
        }

        private void CreateShelveset(string shelvesetName) => gitTfsRemoteMock.Setup(x => x.HasShelveset(shelvesetName)).Returns(true);
    }
}
