using GitTfs.Commands;
using GitTfs.Core;
using Moq;
using StructureMap.AutoMocking;
using Xunit;

namespace GitTfs.Test.Commands
{
    public class ShelveDeleteTest : BaseTest
    {
        private readonly MoqAutoMocker<ShelveDelete> _mocks;

        public ShelveDeleteTest()
        {
            _mocks = new MoqAutoMocker<ShelveDelete>();
        }

        private void InitMocks4Tests(out Mock<IGitRepository> gitRepositoryMock, out Mock<IGitTfsRemote> remoteMock)
        {
            // mock git repository
            gitRepositoryMock = new Mock<IGitRepository>();
            gitRepositoryMock.Setup(r => r.HasRemote(It.IsAny<string>())).Returns(true);
            _mocks.Get<Globals>().Repository = gitRepositoryMock.Object;

            // mock tfs remote
            _mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            remoteMock = new Mock<IGitTfsRemote>();
            gitRepositoryMock.Setup(r => r.ReadTfsRemote(It.IsAny<string>())).Returns(remoteMock.Object);
        }

        [Fact]
        public void ShouldFailIfNoShelvesetNameProvided()
        {
            const string SHELVESET_NAME = "";

            Assert.NotEqual(GitTfsExitCodes.OK, _mocks.ClassUnderTest.Run(SHELVESET_NAME));
        }

        [Fact]
        public void ShouldFailIfInvalidShelvesetNameProvided()
        {
            const string NONEXISTENT_SHELVESET_NAME = "no-such-shelveset";

            InitMocks4Tests(out _, out var remote);
            remote.Setup(r => r.HasShelveset(NONEXISTENT_SHELVESET_NAME)).Returns(false);

            Assert.NotEqual(GitTfsExitCodes.OK, _mocks.ClassUnderTest.Run(NONEXISTENT_SHELVESET_NAME));
        }

        [Fact]
        public void ShouldTellRemoteToDeleteShelveset()
        {
            const string SHELVESET_NAME = "Shelveset name";
            InitMocks4Tests(out var repository, out var remote);
            remote.Setup(r => r.HasShelveset(It.IsAny<string>())).Returns(true);

            _mocks.ClassUnderTest.Run(SHELVESET_NAME);

            remote.Verify(r => r.DeleteShelveset(SHELVESET_NAME), Times.Once);
        }
    }
}