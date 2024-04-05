using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;

using Moq;

using Xunit;

namespace GitTfs.Test.Core
{
    public class GlobalsTests : BaseTest
    {
        private readonly Globals _globals;
        private readonly Mock<IGitRepository> _gitRepositoryMock;
        private readonly ITfsHelper _tfsHelper;

        public GlobalsTests()
        {
            _gitRepositoryMock = new Mock<IGitRepository>();
            _globals = new Globals { Bootstrapper = null, Repository = _gitRepositoryMock.Object };
            _tfsHelper = new Mock<ITfsHelper>().Object;
        }

        [Fact]
        public void WhenUserSpecifyARemote_ThenReturnIt()
        {
            _globals.UserSpecifiedRemoteId = "IWantThatRemote";

            Assert.Equal("IWantThatRemote", _globals.RemoteId);
        }

        [Fact]
        public void WhenOnlyOneRemoteFoundInParentCommits_ThenReturnIt()
        {
            _gitRepositoryMock.Setup(r => r.GetLastParentTfsCommits("HEAD"))
                   .Returns(new List<TfsChangesetInfo>()
                       {
                           new TfsChangesetInfo()
                               {
                                   ChangesetId = 34,
                                   Remote = new GitTfsRemote(new RemoteInfo() {Id = "myRemote"}, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null))
                               }
                       });


            Assert.Equal("myRemote", _globals.RemoteId);
        }

        [Fact]
        public void WhenTwoRemotesFoundInParentCommits_ThenReturnTheFirst()
        {
            _gitRepositoryMock.Setup(r => r.GetLastParentTfsCommits("HEAD"))
                   .Returns(new List<TfsChangesetInfo>()
                       {
                           new TfsChangesetInfo()
                               {
                                   ChangesetId = 34,
                                   Remote = new GitTfsRemote(new RemoteInfo() {Id = "mainRemote"}, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null))
                               },
                               new TfsChangesetInfo()
                               {
                                   ChangesetId = 34,
                                   Remote = new GitTfsRemote(new RemoteInfo() {Id = "myRemote"}, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null))
                               },
                       });

            Assert.Equal("mainRemote", _globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_AndNoRemotesInRepository_ThenReturnDefaultOne()
        {
            _gitRepositoryMock.Setup(r => r.GetLastParentTfsCommits("HEAD"))
                   .Returns(new List<TfsChangesetInfo>());
            _gitRepositoryMock.Setup(r => r.ReadAllTfsRemotes())
                   .Returns(new List<GitTfsRemote>());
            Assert.Equal("default", _globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_ThereIsOnlyOneRemoteInRepository_AndThisIsTheDefaultOne_ThenReturnIt()
        {
            _gitRepositoryMock.Setup(r => r.GetLastParentTfsCommits("HEAD"))
                   .Returns(new List<TfsChangesetInfo>());
            _gitRepositoryMock.Setup(r => r.ReadAllTfsRemotes())
                   .Returns(new List<GitTfsRemote>() { new GitTfsRemote(new RemoteInfo() { Id = "default" }, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null)) });
            Assert.Equal("default", _globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_AndThereIsOnlyOneRemoteInRepository_ThenThrowAnException()
        {
            _gitRepositoryMock.Setup(r => r.GetLastParentTfsCommits("HEAD"))
                   .Returns(new List<TfsChangesetInfo>());
            _gitRepositoryMock.Setup(r => r.ReadAllTfsRemotes())
                   .Returns(new List<GitTfsRemote>() { new GitTfsRemote(new RemoteInfo() { Id = "myRemote" }, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null)) });
            Assert.Throws<GitTfsException>(() => _globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_AndThereIsARemoteInRepository_ThenThrowAnException()
        {
            _gitRepositoryMock.Setup(r => r.GetLastParentTfsCommits("HEAD"))
                   .Returns(new List<TfsChangesetInfo>());
            _gitRepositoryMock.Setup(r => r.ReadAllTfsRemotes())
                   .Returns(new List<GitTfsRemote>()
                       {
                           new GitTfsRemote(new RemoteInfo() { Id = "myRemote" }, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null)),
                           new GitTfsRemote(new RemoteInfo() { Id = "myRemote2" }, _gitRepositoryMock.Object, new RemoteOptions(), _globals, _tfsHelper, new ConfigProperties(null))
                       });
            Assert.Throws<GitTfsException>(() => _globals.RemoteId);
        }
    }
}