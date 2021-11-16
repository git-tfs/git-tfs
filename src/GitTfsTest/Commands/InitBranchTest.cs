using System;
using System.Collections.Generic;
using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using GitTfs.VsFake;
using Moq;
using StructureMap.AutoMocking;
using Xunit;

namespace GitTfs.Test.Commands
{
    public class InitBranchTest : BaseTest
    {
        #region Test Init
        private readonly MoqAutoMocker<InitBranch> mocks;

        public InitBranchTest()
        {
            mocks = new MoqAutoMocker<InitBranch>();
            var globals = mocks.Get<Globals>();
            var globalsMock = Mock.Get(globals).SetupAllProperties();
            globals.Repository = mocks.Get<IGitRepository>();
        }

        private void InitMocks4Tests(string gitBranchToInit, out Mock<IGitRepository> gitRepositoryMock,
            out Mock<IGitTfsRemote> trunkGitTfsRemoteMock, out Mock<IGitTfsRemote> newBranchRemoteMock, out Mock<ITfsHelper> tfsHelperMock)
        {
            var tfsHelper = mocks.Get<ITfsHelper>();
            tfsHelperMock = Mock.Get(tfsHelper);

            gitRepositoryMock = Mock.Get(mocks.Get<IGitRepository>()).SetupAllProperties();
            gitRepositoryMock.Name = nameof(gitRepositoryMock);
            gitRepositoryMock.SetupGet(r => r.GitDir).Returns(".");
            gitRepositoryMock.Setup(r => r.HasRemote(It.IsAny<string>())).Returns(true);

            var globals = mocks.Get<Globals>();
            globals.Repository = gitRepositoryMock.Object;
            globals.GitDir = ".git";

            trunkGitTfsRemoteMock = new Mock<IGitTfsRemote>().SetupAllProperties();
            trunkGitTfsRemoteMock.SetupGet(p => p.Tfs).Returns(tfsHelper);
            trunkGitTfsRemoteMock.Name = nameof(trunkGitTfsRemoteMock);
            var trunkGitTfsRemote = trunkGitTfsRemoteMock.Object;
            trunkGitTfsRemote.TfsUsername = "user";
            trunkGitTfsRemote.TfsPassword = "pwd";
            trunkGitTfsRemoteMock.SetupGet(x => x.TfsRepositoryPath).Returns("$/MyProject/Trunk");
            trunkGitTfsRemoteMock.SetupGet(x => x.TfsUrl).Returns("http://myTfsServer:8080/tfs");

            newBranchRemoteMock = Mock.Get(mocks.Get<IGitTfsRemote>()).SetupAllProperties();
            newBranchRemoteMock.Name = nameof(newBranchRemoteMock);
            newBranchRemoteMock.SetupGet(r => r.Id).Returns(gitBranchToInit);
        }
        #endregion

        #region Init a Branch
        [Fact]
        public void ShouldInitBranchWhenNoBranchGitNameProposed()
        {
            ShouldInitBranch(null);
        }

        [Fact]
        public void ShouldInitBranchWhenBranchGitNameProposed()
        {
            ShouldInitBranch("MyBranch");
        }

        private void ShouldInitBranch(string expectedGitBranchName)
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            InitMocks4Tests(GIT_BRANCH_TO_INIT, out var gitRepositoryMock, out var trunkGitTfsRemoteMock, out var newBranchRemoteMock, out var tfsHelperMock);

            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch", -1, null)).Returns(new List<RootBranch>() { new RootBranch(2010, "$/MyProject/MyBranch") });

            trunkGitTfsRemoteMock.Name = nameof(trunkGitTfsRemoteMock);
            trunkGitTfsRemoteMock.Setup(t => t.InitBranch(It.IsAny<RemoteOptions>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IRenameResult>())).Returns(newBranchRemoteMock.Object).Verifiable();
            trunkGitTfsRemoteMock.SetupGet(x => x.Tfs).Returns(tfsHelperMock.Object);

            gitRepositoryMock.Name = nameof(gitRepositoryMock);
            gitRepositoryMock.Setup(x => x.ReadTfsRemote("default")).Returns(trunkGitTfsRemoteMock.Object).Verifiable();
            gitRepositoryMock.Setup(x => x.ReadAllTfsRemotes()).Returns(new List<IGitTfsRemote> {trunkGitTfsRemoteMock.Object})
                .Verifiable();

            //newBranchRemoteMock.SetupGet(r => r.RemoteRef).Returns("refs/remote/tfs/" + GIT_BRANCH_TO_INIT).Verifiable();
            newBranchRemoteMock.Name = nameof(newBranchRemoteMock);
            newBranchRemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>()))
                .Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            newBranchRemoteMock.Object.MaxCommitHash = "sha1AfterFetch";

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("$/MyProject/MyBranch", expectedGitBranchName));

            gitRepositoryMock.Verify();
            trunkGitTfsRemoteMock.Verify();
            newBranchRemoteMock.Verify();
        }

        [Fact]
        public void ShouldDoNothingBecauseRemoteAlreadyExisting()
        {
            const string GIT_BRANCH_TO_INIT = "myBranch";

            InitMocks4Tests(GIT_BRANCH_TO_INIT, out var gitRepository, out var trunkGitTfsRemoteMock, out var newBranchRemoteMock, out var tfsHelperMock);

            trunkGitTfsRemoteMock.Name = nameof(trunkGitTfsRemoteMock);
            trunkGitTfsRemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true });
            trunkGitTfsRemoteMock.Setup(t => t.InitBranch(It.IsAny<RemoteOptions>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IRenameResult>())).Returns(newBranchRemoteMock.Object);

            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch", -1, null)).Returns(new List<RootBranch>() { new RootBranch(2010, "$/MyProject/MyBranch") });

            TfsHelper tfsHelper = new TfsHelper(mocks.Container, null);
            Mock<IGitTfsRemote> gitTfsRemoteMock = new Mock<IGitTfsRemote>().SetupAllProperties();
            gitTfsRemoteMock.SetupGet(x => x.Tfs).Returns(tfsHelper);
            IGitTfsRemote existingBranchRemote = gitTfsRemoteMock.Object;
            existingBranchRemote.TfsUsername = "user";
            existingBranchRemote.TfsPassword = "pwd";
            gitTfsRemoteMock.SetupGet(x => x.TfsRepositoryPath).Returns("$/MyProject/MyBranch");
            gitTfsRemoteMock.SetupGet(x => x.TfsUrl).Returns("http://myTfsServer:8080/tfs");

            gitRepository.Name = nameof(gitRepository);
            gitRepository.Setup(x => x.ReadTfsRemote("default")).Returns(trunkGitTfsRemoteMock.Object).Verifiable();
            gitRepository.Setup(x => x.ReadAllTfsRemotes()).Returns(new List<IGitTfsRemote> { trunkGitTfsRemoteMock.Object, existingBranchRemote }).Verifiable();

            newBranchRemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>()))
                .Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("$/MyProject/MyBranch", GIT_BRANCH_TO_INIT));

            gitRepository.Verify(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT), Times.Never);
            gitRepository.Verify();
            trunkGitTfsRemoteMock.Verify();
            newBranchRemoteMock.Verify();
        }

        [Fact]
        public void ShouldFailBecauseRootChangeSetNotFound()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            InitMocks4Tests(GIT_BRANCH_TO_INIT, out var gitRepository, out var trunkGitTfsRemoteMock, out var newBranchRemote, out var tfsHelperMock);

            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch", -1, null)).Throws(new GitTfsException(""));

            gitRepository.Setup(x => x.ReadTfsRemote("default")).Returns(trunkGitTfsRemoteMock.Object).Verifiable();
            gitRepository.Setup(x => x.ReadAllTfsRemotes()).Returns(new List<IGitTfsRemote> { trunkGitTfsRemoteMock.Object }).Verifiable();

            Assert.Throws<GitTfsException>(() => mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));

            gitRepository.Verify(x => x.CommandOneline(It.IsAny<string[]>()), Times.Never);
            gitRepository.Verify();
        }

        [Fact]
        public void ShouldFailBecauseGitCommitCorrespondingToChangeSetNotFound()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            InitMocks4Tests(GIT_BRANCH_TO_INIT, out var gitRepository, out var remote, out var newBranchRemote, out var tfsHelperMock);

            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch", -1, null)).Returns(new List<RootBranch>() { new RootBranch(2010, "$/MyProject/MyBranch") });

            gitRepository.Setup(x => x.ReadTfsRemote("default")).Returns(remote.Object).Verifiable();
            gitRepository.Setup(x => x.ReadAllTfsRemotes()).Returns(new List<IGitTfsRemote> { remote.Object }).Verifiable();

            var ex = Assert.Throws<GitTfsException>(() => mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));
            Assert.Equal("error: Couldn't fetch parent branch\n", ex.Message);

            gitRepository.Verify(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT), Times.Never);
            gitRepository.Verify();
        }


        #endregion

        #region Init All branches

        [Fact]
        public void ShouldInitAllBranches()
        {
            const string GIT_BRANCH_TO_INIT1 = "MyBranch1";
            const string GIT_BRANCH_TO_INIT2 = "MyBranch2";

            InitMocks4Tests(GIT_BRANCH_TO_INIT1, out var gitRepositoryMock, out var trunkGitTfsRemote, out var newBranch1RemoteMock, out var tfsHelperMock);

            mocks.ClassUnderTest.CloneAllBranches = true;
            var tfsPathBranch1 = "$/MyProject/MyBranch1";
            var tfsPathBranch2 = "$/MyProject/MyBranch2";
            tfsHelperMock.Setup(t => t.GetBranches(true)).Returns(new IBranchObject[] {
                new MockBranchObject() { IsRoot = true, Path = trunkGitTfsRemote.Object.TfsRepositoryPath },
                new MockBranchObject() { ParentPath = trunkGitTfsRemote.Object.TfsRepositoryPath, Path = tfsPathBranch1 },
                new MockBranchObject() { ParentPath = trunkGitTfsRemote.Object.TfsRepositoryPath, Path = tfsPathBranch2 },
            });
            tfsHelperMock.Setup(t => t.GetAllTfsRootBranchesOrderedByCreation()).Returns(new List<string> { trunkGitTfsRemote.Object.TfsRepositoryPath });

            gitRepositoryMock.Name = nameof(gitRepositoryMock);
            gitRepositoryMock.Setup(x => x.ReadTfsRemote("default")).Returns(trunkGitTfsRemote.Object).Verifiable();
            gitRepositoryMock.Setup(x => x.ReadAllTfsRemotes()).Returns(new List<IGitTfsRemote> { trunkGitTfsRemote.Object });

            #region Branch1
            var rootChangeSetB1 = 1000;
            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch(tfsPathBranch1, -1, null)).Returns(new List<RootBranch>() { new RootBranch(rootChangeSetB1, tfsPathBranch1) });

            newBranch1RemoteMock.Name = nameof(newBranch1RemoteMock);
            newBranch1RemoteMock.Setup(r => r.RemoteRef).Returns("refs/remote/tfs/" + GIT_BRANCH_TO_INIT1);//.Verifiable();
            newBranch1RemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            newBranch1RemoteMock.Object.MaxCommitHash = "ShaAfterFetch_Branch1";
            #endregion

            #region Branch2
            var newBranch2RemoteMock = new Mock<IGitTfsRemote>();
            newBranch2RemoteMock.Name = nameof(newBranch2RemoteMock);
            newBranch2RemoteMock.SetupGet(r => r.Id).Returns(GIT_BRANCH_TO_INIT2);

            var rootChangeSetB2 = 2000;
            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch(tfsPathBranch2, -1, null)).Returns(new List<RootBranch>() { new RootBranch(rootChangeSetB2, tfsPathBranch2) });

            newBranch2RemoteMock.Name = nameof(newBranch2RemoteMock);
            newBranch2RemoteMock.Setup(r => r.RemoteRef).Returns("refs/remote/tfs/" + GIT_BRANCH_TO_INIT2);//.Verifiable();
            newBranch2RemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            newBranch2RemoteMock.Object.MaxCommitHash = "ShaAfterFetch_Branch2";
            #endregion

            trunkGitTfsRemote.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            trunkGitTfsRemote.Setup(t => t.InitBranch(It.IsAny<RemoteOptions>(), tfsPathBranch1, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IRenameResult>())).Returns(newBranch1RemoteMock.Object);
            trunkGitTfsRemote.Setup(t => t.InitBranch(It.IsAny<RemoteOptions>(), tfsPathBranch2, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IRenameResult>())).Returns(newBranch2RemoteMock.Object);
            trunkGitTfsRemote.Object.MaxChangesetId = 2000; //Simulate fetch already done
            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run());

            gitRepositoryMock.Verify();
            newBranch1RemoteMock.Verify();
            newBranch2RemoteMock.Verify();
        }

        [Fact]
        public void WhenCloningASubBranch_ThenInitAllBranchesShouldSucceedWithInitializingOnlyChildrenBranches()
        {
            const string GIT_BRANCH_TO_INIT1 = "MyBranch1";
            const string GIT_BRANCH_TO_INIT2 = "MyBranch2";
            const string GIT_BRANCH_TO_INIT3 = "MyBranch3";

            InitMocks4Tests(GIT_BRANCH_TO_INIT1, out var gitRepository, out var trunkGitTfsRemote, out var newBranch1RemoteMock, out var tfsHelperMock);

            mocks.ClassUnderTest.CloneAllBranches = true;
            var tfsPathBranch1 = "$/MyProject/MyBranch1";
            var tfsPathBranch2 = "$/MyProject/MyBranch2";
            var tfsPathBranch3 = "$/MyProject/MyBranch3";
            tfsHelperMock.Setup(t => t.GetBranches(true)).Returns(new IBranchObject[] {
                new MockBranchObject() { IsRoot = true, Path = "$/MyProject/ParentOfTrunk" },
                new MockBranchObject() { ParentPath = "$/MyProject/ParentOfTrunk", Path = trunkGitTfsRemote.Object.TfsRepositoryPath },
                new MockBranchObject() { ParentPath = trunkGitTfsRemote.Object.TfsRepositoryPath, Path = tfsPathBranch1 },
                new MockBranchObject() { ParentPath = trunkGitTfsRemote.Object.TfsRepositoryPath, Path = tfsPathBranch2 },
                new MockBranchObject() { ParentPath = "$/MyProject/ParentOfTrunk", Path = tfsPathBranch3 },
            });

            gitRepository.Name = nameof(gitRepository);
            gitRepository.Setup(x => x.ReadTfsRemote("default")).Returns(trunkGitTfsRemote.Object).Verifiable();
            gitRepository.Setup(x => x.ReadAllTfsRemotes()).Returns(new List<IGitTfsRemote> { trunkGitTfsRemote.Object });

            #region Branch1
            var rootChangeSetB1 = 1000;
            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch(tfsPathBranch1, -1, null)).Returns(new List<RootBranch>() { new RootBranch(rootChangeSetB1, tfsPathBranch1) });

            newBranch1RemoteMock.Name = nameof(newBranch1RemoteMock);
            newBranch1RemoteMock.Setup(r => r.RemoteRef).Returns("refs/remote/tfs/" + GIT_BRANCH_TO_INIT1);//.Verifiable();
            newBranch1RemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            newBranch1RemoteMock.Object.MaxCommitHash = "ShaAfterFetch_Branch1";
            #endregion

            #region Branch2
            var newBranch2RemoteMock = new Mock<IGitTfsRemote>();
            newBranch2RemoteMock.SetupGet(r => r.Id).Returns(GIT_BRANCH_TO_INIT2);

            var rootChangeSetB2 = 2000;
            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch(tfsPathBranch2, -1, null)).Returns(new List<RootBranch>() { new RootBranch(rootChangeSetB2, tfsPathBranch2) });

            newBranch2RemoteMock.Name = nameof(newBranch2RemoteMock);
            newBranch2RemoteMock.Setup(r => r.RemoteRef).Returns("refs/remote/tfs/" + GIT_BRANCH_TO_INIT2);//.Verifiable();
            newBranch2RemoteMock.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            newBranch2RemoteMock.Object.MaxCommitHash = "ShaAfterFetch_Branch2";
            #endregion


            trunkGitTfsRemote.Setup(r => r.Fetch(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<IRenameResult>())).Returns(new GitTfsRemote.FetchResult() { IsSuccess = true }).Verifiable();
            trunkGitTfsRemote.Setup(t => t.InitBranch(It.IsAny<RemoteOptions>(), tfsPathBranch1, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IRenameResult>())).Returns(newBranch1RemoteMock.Object);
            trunkGitTfsRemote.Setup(t => t.InitBranch(It.IsAny<RemoteOptions>(), tfsPathBranch2, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IRenameResult>())).Returns(newBranch2RemoteMock.Object);
            trunkGitTfsRemote.Object.MaxChangesetId = 2000; //Simulate fetch already done
            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run());

            gitRepository.Verify(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT3), Times.Never);
            gitRepository.Verify();

            newBranch1RemoteMock.Verify();
            newBranch2RemoteMock.Verify();
        }

        [Fact]
        public void ShouldFailInitAllBranchesBecauseNoFetchWasSpecified()
        {
            mocks.ClassUnderTest.CloneAllBranches = true;
            mocks.ClassUnderTest.NoFetch = true;

            var ex = Assert.Throws<GitTfsException>(() => mocks.ClassUnderTest.Run());
            Assert.Equal("error: --no-fetch cannot be used with --all", ex.Message);
        }

        [Fact]
        public void ShouldFailInitAllBranchesBecauseCloneWasNotMadeFromABranch()
        {
            const string GIT_BRANCH_TO_INIT1 = "MyBranch1";
            const string GIT_BRANCH_TO_INIT2 = "MyBranch2";

            InitMocks4Tests(GIT_BRANCH_TO_INIT1, out var gitRepositoryMock, out var trunkGitTfsRemoteMock, out var newBranch1RemoteMock, out var tfsHelperMock);

            mocks.ClassUnderTest.CloneAllBranches = true;
            var tfsPathBranch1 = "$/MyProject/MyBranch1";
            var tfsPathBranch2 = "$/MyProject/MyBranch2";
            tfsHelperMock.Setup(t => t.GetBranches(false)).Returns(new IBranchObject[] {
                new MockBranchObject() { IsRoot = true, Path = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!" },
                new MockBranchObject() { ParentPath = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!", Path = tfsPathBranch1 },
                new MockBranchObject() { ParentPath = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!", Path = tfsPathBranch2 },
                // Note the remote.Object TfsRepositoryPath is NOT included!
            });

            gitRepositoryMock.Setup(x => x.ReadTfsRemote("default")).Returns(trunkGitTfsRemoteMock.Object).Verifiable();

            #region Branch1
            var rootChangeSetB1 = 1000;
            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch(tfsPathBranch1, -1, null)).Returns(new List<RootBranch>() { new RootBranch(rootChangeSetB1, tfsPathBranch1) });

            #endregion

            #region Branch2
            var newBranch2RemoteMock = new Mock<IGitTfsRemote>();
            newBranch2RemoteMock.SetupGet(r => r.Id).Returns(GIT_BRANCH_TO_INIT2);

            var rootChangeSetB2 = 2000;
            tfsHelperMock.Setup(t => t.GetRootChangesetForBranch(tfsPathBranch2, -1, null)).Returns(new List<RootBranch>() { new RootBranch(rootChangeSetB2, tfsPathBranch2) });

            #endregion

            var ex = Assert.Throws<GitTfsException>(() => mocks.ClassUnderTest.Run());

            Assert.Equal("error: The use of the option '--branches=all' to init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '$/MyProject/Trunk' is not a TFS branch!", ex.Message);

            gitRepositoryMock.Verify(x => x.ReadAllTfsRemotes(), Times.Never);
            gitRepositoryMock.Verify(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT1), Times.Never);
            gitRepositoryMock.Verify(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT2), Times.Never);

            gitRepositoryMock.Verify();

            newBranch1RemoteMock.Verify();
            newBranch2RemoteMock.Verify();
        }

        #endregion

        #region Help Command
        [Fact]
        public void ShouldCallCommandHelp()
        {
            var gitRepositoryMock = new Mock<IGitRepository>();
            mocks.Get<Globals>().Repository = gitRepositoryMock.Object;
            var remoteMock = new Mock<IGitTfsRemote>();
            remoteMock.SetupAllProperties();
            var remote = remoteMock.Object;
            remote.TfsUsername = "user";
            remote.TfsPassword = "pwd";
            remoteMock.SetupGet(x => x.TfsRepositoryPath).Returns("$/MyProject/Trunk");
            remoteMock.SetupGet(x => x.TfsUrl).Returns("http://myTfsServer:8080/tfs");
            remoteMock.SetupGet(x => x.Tfs).Returns(new TfsHelper(mocks.Container, null));

            //Not Very Clean!!! Don't know how to test that :(
            //If the InvalidOperationException is thrown, it's that the Helper.Run() is Called => That's what is expected!
            Assert.Throws<InvalidOperationException>(() => mocks.ClassUnderTest.Run());
            gitRepositoryMock.Verify(x => x.ReadTfsRemote("default"), Times.Never);
        }
        #endregion
    }
}
