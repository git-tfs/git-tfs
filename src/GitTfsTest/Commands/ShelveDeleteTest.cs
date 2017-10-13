using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Commands
{
    public class ShelveDeleteTest : BaseTest
    {
        private readonly RhinoAutoMocker<ShelveDelete> _mocks;

        public ShelveDeleteTest()
        {
            _mocks = new RhinoAutoMocker<ShelveDelete>();
        }

        private void InitMocks4Tests(out IGitRepository gitRepository, out IGitTfsRemote remote)
        {
            // mock git repository
            gitRepository = _mocks.Get<IGitRepository>();
            gitRepository.Stub(r => r.HasRemote(Arg<string>.Is.Anything)).Return(true);
            _mocks.Get<Globals>().Repository = gitRepository;

            // mock tfs remote
            _mocks.Get<Globals>().UserSpecifiedRemoteId = "default";
            remote = MockRepository.GenerateStub<IGitTfsRemote>();
            gitRepository.Stub(r => r.ReadTfsRemote(Arg<string>.Is.Anything)).Return(remote);
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

            IGitRepository repository; IGitTfsRemote remote;
            InitMocks4Tests(out repository, out remote);
            remote.Stub(r => r.HasShelveset(NONEXISTENT_SHELVESET_NAME)).Return(false);

            Assert.NotEqual(GitTfsExitCodes.OK, _mocks.ClassUnderTest.Run(NONEXISTENT_SHELVESET_NAME));
        }

        [Fact]
        public void ShouldTellRemoteToDeleteShelveset()
        {
            const string SHELVESET_NAME = "Shelveset name";
            IGitRepository repository; IGitTfsRemote remote;
            InitMocks4Tests(out repository, out remote);
            remote.Stub(r => r.HasShelveset(Arg<string>.Is.Anything)).Return(true);

            _mocks.ClassUnderTest.Run(SHELVESET_NAME);

            remote.AssertWasCalled(
                x => x.DeleteShelveset(null),
                y => y.Constraints(Is.Equal(SHELVESET_NAME)));
        }
    }
}