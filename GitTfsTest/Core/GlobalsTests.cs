using System.Collections.Generic;
using System.IO;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class GlobalsTests
    {
        [Fact]
        public void WhenUserSpecifyARemote_ThenReturnIt()
        {
            var mocker = new RhinoAutoMocker<IGitRepository>();
            var gitRepoMock = mocker.Get<IGitRepository>();
            var globals = new Globals() { Bootstrapper = null, Stdout = new StringWriter(), Repository = gitRepoMock };
            globals.UserSpecifiedRemoteId = "IWantThatRemote";
            Assert.Equal("IWantThatRemote", globals.RemoteId);
        }

        [Fact]
        public void WhenOnlyOneRemoteFoundInParentCommits_ThenReturnIt()
        {
            var mocker = new RhinoAutoMocker<IGitRepository>();
            var gitRepoMock = mocker.Get<IGitRepository>();
            var globals = new Globals() { Bootstrapper = null, Stdout = new StringWriter(), Repository = gitRepoMock };
            globals.Repository.Stub(r => r.GetLastParentTfsCommits("HEAD"))
                   .Return(new List<TfsChangesetInfo>()
                       {
                           new TfsChangesetInfo()
                               {
                                   ChangesetId = 34,
                                   Remote = new GitTfsRemote(new RemoteInfo() {Id = "myRemote"}, gitRepoMock, new RemoteOptions(), globals, mocker.Get<ITfsHelper>(), new StringWriter())
                               }
                       });
            Assert.Equal("myRemote", globals.RemoteId);
        }

        [Fact]
        public void WhenTwoRemotesFoundInParentCommits_ThenReturnTheFirst()
        {
            var mocker = new RhinoAutoMocker<IGitRepository>();
            var gitRepoMock = mocker.Get<IGitRepository>();
            var globals = new Globals() { Bootstrapper = null, Stdout = new StringWriter(), Repository = gitRepoMock };
            globals.Repository.Stub(r => r.GetLastParentTfsCommits("HEAD"))
                   .Return(new List<TfsChangesetInfo>()
                       {
                           new TfsChangesetInfo()
                               {
                                   ChangesetId = 34,
                                   Remote = new GitTfsRemote(new RemoteInfo() {Id = "mainRemote"}, gitRepoMock, new RemoteOptions(), globals, mocker.Get<ITfsHelper>(), new StringWriter())
                               },
                               new TfsChangesetInfo()
                               {
                                   ChangesetId = 34,
                                   Remote = new GitTfsRemote(new RemoteInfo() {Id = "myRemote"}, gitRepoMock, new RemoteOptions(), globals, mocker.Get<ITfsHelper>(), new StringWriter())
                               },
                       });
            Assert.Equal("mainRemote", globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_AndNoRemotesInRepository_ThenReturnDefaultOne()
        {
            var mocker = new RhinoAutoMocker<IGitRepository>();
            var gitRepoMock = mocker.Get<IGitRepository>();
            var globals = new Globals() { Bootstrapper = null, Stdout = new StringWriter(), Repository = gitRepoMock };
            globals.Repository.Stub(r => r.GetLastParentTfsCommits("HEAD"))
                   .Return(new List<TfsChangesetInfo>());
            globals.Repository.Stub(r => r.ReadAllTfsRemotes())
                   .Return(new List<GitTfsRemote>());
            Assert.Equal("default", globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_AndThereIsOnlyOneRemoteInRepository_ThenReturnIt()
        {
            var mocker = new RhinoAutoMocker<IGitRepository>();
            var gitRepoMock = mocker.Get<IGitRepository>();
            var globals = new Globals() { Bootstrapper = null, Stdout = new StringWriter(), Repository = gitRepoMock };
            globals.Repository.Stub(r => r.GetLastParentTfsCommits("HEAD"))
                   .Return(new List<TfsChangesetInfo>());
            globals.Repository.Stub(r => r.ReadAllTfsRemotes())
                   .Return(new List<GitTfsRemote>() { new GitTfsRemote(new RemoteInfo() { Id = "myRemote" }, gitRepoMock, new RemoteOptions(), globals, mocker.Get<ITfsHelper>(), new StringWriter()) });
            Assert.Equal("myRemote", globals.RemoteId);
        }

        [Fact]
        public void WhenNoRemotesFoundInParentCommits_AndThereIsARemoteInRepository_ThenThrowAnException()
        {
            var mocker = new RhinoAutoMocker<IGitRepository>();
            var gitRepoMock = mocker.Get<IGitRepository>();
            var globals = new Globals() { Bootstrapper = null, Stdout = new StringWriter(), Repository = gitRepoMock };
            globals.Repository.Stub(r => r.GetLastParentTfsCommits("HEAD"))
                   .Return(new List<TfsChangesetInfo>());
            globals.Repository.Stub(r => r.ReadAllTfsRemotes())
                   .Return(new List<GitTfsRemote>()
                       {
                           new GitTfsRemote(new RemoteInfo() { Id = "myRemote" }, gitRepoMock, new RemoteOptions(), globals, mocker.Get<ITfsHelper>(), new StringWriter()),
                           new GitTfsRemote(new RemoteInfo() { Id = "myRemote2" }, gitRepoMock, new RemoteOptions(), globals, mocker.Get<ITfsHelper>(), new StringWriter())
                       });
            Assert.Throws(typeof(GitTfsException), () => globals.RemoteId);
        }
    }
}