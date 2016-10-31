using System;
using System.Collections.Generic;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.VsFake;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Commands
{
    public class InitBranchTest : BaseTest
    {
        #region Test Init
        private readonly RhinoAutoMocker<InitBranch> mocks;

        public InitBranchTest()
        {
            mocks = new RhinoAutoMocker<InitBranch>();
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
        }

        private void InitMocks4Tests(string gitBranchToInit, out IGitRepository gitRepository, out IGitTfsRemote remote, out IGitTfsRemote newBranchRemote)
        {
            gitRepository = mocks.Get<IGitRepository>();
            mocks.Get<Globals>().Repository = gitRepository;
            mocks.Get<Globals>().GitDir = ".git";
            remote = MockRepository.GenerateStub<IGitTfsRemote>();
            remote.TfsUsername = "user";
            remote.TfsPassword = "pwd";
            remote.TfsRepositoryPath = "$/MyProject/Trunk";
            remote.TfsUrl = "http://myTfsServer:8080/tfs";
            remote.Tfs = new TfsHelper(mocks.Container, null);
            gitRepository.Stub(r => r.GitDir).Return(".");
            gitRepository.Stub(r => r.HasRemote(Arg<string>.Is.Anything)).Return(true);

            newBranchRemote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranchRemote.Id = gitBranchToInit;
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

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(new List<RootBranch>() { new RootBranch(2010, "$/MyProject/MyBranch") });
            var mockRemote = MockRepository.GenerateStub<IGitTfsRemote>();
            mockRemote.Stub(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true });
            remote.Stub(t => t.InitBranch(Arg<RemoteOptions>.Is.Anything, Arg<string>.Is.Anything, Arg<int>.Is.Anything, Arg<bool>.Is.Anything, Arg<string>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(mockRemote);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();

            newBranchRemote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT).Repeat.Once();
            newBranchRemote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            newBranchRemote.MaxCommitHash = "sha1AfterFetch";

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("$/MyProject/MyBranch", expectedGitBranchName));

            gitRepository.VerifyAllExpectations();
            newBranchRemote.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldDoNothingBecauseRemoteAlreadyExisting()
        {
            const string GIT_BRANCH_TO_INIT = "myBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            var mockRemote = MockRepository.GenerateStub<IGitTfsRemote>();
            mockRemote.Stub(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true });
            remote.Stub(t => t.InitBranch(Arg<RemoteOptions>.Is.Anything, Arg<string>.Is.Anything, Arg<int>.Is.Anything, Arg<bool>.Is.Anything, Arg<string>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(mockRemote);
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(new List<RootBranch>() { new RootBranch(2010, "$/MyProject/MyBranch") });

            IGitTfsRemote existingBranchRemote = MockRepository.GenerateStub<IGitTfsRemote>();
            existingBranchRemote.TfsUsername = "user";
            existingBranchRemote.TfsPassword = "pwd";
            existingBranchRemote.TfsRepositoryPath = "$/MyProject/MyBranch";
            existingBranchRemote.TfsUrl = "http://myTfsServer:8080/tfs";
            existingBranchRemote.Tfs = new TfsHelper(mocks.Container, null);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote, existingBranchRemote }).Repeat.Once();

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Never();

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("$/MyProject/MyBranch", GIT_BRANCH_TO_INIT));

            gitRepository.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldFailBecauseRootChangeSetNotFound()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Throw(new GitTfsException(""));

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.CommandOneline(Arg<string[]>.Is.Anything)).Return("foo!").Repeat.Never();

            Assert.Throws(typeof(GitTfsException), () => mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));

            gitRepository.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldFailBecauseGitCommitCorrespondingToChangeSetNotFound()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(new List<RootBranch>() { new RootBranch(2010, "$/MyProject/MyBranch") });

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Never();

            var ex = Assert.Throws(typeof(GitTfsException), () => mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));
            Assert.Equal("error: Couldn't fetch parent branch\n", ex.Message);

            gitRepository.VerifyAllExpectations();
        }


        #endregion

        #region Init a branch (2008 compatibility mode)
        [Fact]
        public void ShouldInitBranchInTfs2008CompatibilityMode()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            var mockRemote = MockRepository.GenerateStub<IGitTfsRemote>();
            mockRemote.Stub(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true });
            remote.Stub(t => t.InitBranch(Arg<RemoteOptions>.Is.Anything, Arg<string>.Is.Anything, Arg<int>.Is.Anything, Arg<bool>.Is.Anything, Arg<string>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(mockRemote);
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch", -1, remote.TfsRepositoryPath)).Return(new List<RootBranch>() { new RootBranch(2008, "$/MyProject/MyBranch") });

            mocks.ClassUnderTest.ParentBranch = remote.TfsRepositoryPath;

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();

            newBranchRemote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT).Repeat.Once();
            newBranchRemote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            newBranchRemote.MaxCommitHash = "sha1AfterFetch";

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));

            gitRepository.VerifyAllExpectations();
            newBranchRemote.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldFailedInitBranchInTfs2008CompatibilityModeBecauseParentBranchNotAlreadyCloned()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(new List<RootBranch>() { new RootBranch(2008, "$/MyProject/MyBranch") });

            mocks.ClassUnderTest.ParentBranch = "$/MyProject/MyParentBranchNotAlreadyCloned";

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Never();

            Assert.Throws(typeof(GitTfsException), () => mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));

            gitRepository.VerifyAllExpectations();
        }
        #endregion

        #region Init All branches

        [Fact]
        public void ShouldInitAllBranches()
        {
            const string GIT_BRANCH_TO_INIT1 = "MyBranch1";
            const string GIT_BRANCH_TO_INIT2 = "MyBranch2";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranch1Remote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT1, out gitRepository, out remote, out newBranch1Remote);

            mocks.ClassUnderTest.CloneAllBranches = true;
            remote.Tfs = mocks.Get<ITfsHelper>();
            var tfsPathBranch1 = "$/MyProject/MyBranch1";
            var tfsPathBranch2 = "$/MyProject/MyBranch2";
            remote.Tfs.Stub(t => t.GetBranches()).Return(new IBranchObject[] {
                new MockBranchObject() { IsRoot = true, Path = remote.TfsRepositoryPath },
                new MockBranchObject() { ParentPath = remote.TfsRepositoryPath, Path = tfsPathBranch1 },
                new MockBranchObject() { ParentPath = remote.TfsRepositoryPath, Path = tfsPathBranch2 },
            });
            remote.Tfs.Stub(t => t.GetAllTfsRootBranchesOrderedByCreation()).Return(new List<string> { remote.TfsRepositoryPath });

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Stub(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote });

            #region Branch1
            var rootChangeSetB1 = 1000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch1)).Return(new List<RootBranch>() { new RootBranch(rootChangeSetB1, tfsPathBranch1) });

            newBranch1Remote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT1).Repeat.Once();
            newBranch1Remote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            newBranch1Remote.MaxCommitHash = "ShaAfterFetch_Branch1";
            #endregion

            #region Branch2
            var newBranch2Remote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranch2Remote.Id = GIT_BRANCH_TO_INIT2;

            var rootChangeSetB2 = 2000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch2)).Return(new List<RootBranch>() { new RootBranch(rootChangeSetB2, tfsPathBranch2) });

            newBranch2Remote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT2).Repeat.Once();
            newBranch2Remote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            newBranch2Remote.MaxCommitHash = "ShaAfterFetch_Branch2";
            #endregion

            remote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run());

            gitRepository.VerifyAllExpectations();
            newBranch1Remote.VerifyAllExpectations();
            newBranch2Remote.VerifyAllExpectations();
        }

        [Fact]
        public void WhenCloningASubBranch_ThenInitAllBranchesShouldSucceedWithInitializingOnlyChildrenBranches()
        {
            const string GIT_BRANCH_TO_INIT1 = "MyBranch1";
            const string GIT_BRANCH_TO_INIT2 = "MyBranch2";
            const string GIT_BRANCH_TO_INIT3 = "MyBranch3";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranch1Remote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT1, out gitRepository, out remote, out newBranch1Remote);

            mocks.ClassUnderTest.CloneAllBranches = true;
            remote.Tfs = mocks.Get<ITfsHelper>();
            var tfsPathBranch1 = "$/MyProject/MyBranch1";
            var tfsPathBranch2 = "$/MyProject/MyBranch2";
            var tfsPathBranch3 = "$/MyProject/MyBranch3";
            remote.Tfs.Stub(t => t.GetBranches()).Return(new IBranchObject[] {
                new MockBranchObject() { IsRoot = true, Path = "$/MyProject/ParentOfTrunk" },
                new MockBranchObject() { ParentPath = "$/MyProject/ParentOfTrunk", Path = remote.TfsRepositoryPath },
                new MockBranchObject() { ParentPath = remote.TfsRepositoryPath, Path = tfsPathBranch1 },
                new MockBranchObject() { ParentPath = remote.TfsRepositoryPath, Path = tfsPathBranch2 },
                new MockBranchObject() { ParentPath = "$/MyProject/ParentOfTrunk", Path = tfsPathBranch3 },
            });

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Stub(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote });

            #region Branch1
            var rootChangeSetB1 = 1000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch1)).Return(new List<RootBranch>() { new RootBranch(rootChangeSetB1, tfsPathBranch1) });

            newBranch1Remote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT1).Repeat.Once();
            newBranch1Remote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            newBranch1Remote.MaxCommitHash = "ShaAfterFetch_Branch1";
            #endregion

            #region Branch2
            var newBranch2Remote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranch2Remote.Id = GIT_BRANCH_TO_INIT2;

            var rootChangeSetB2 = 2000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch2)).Return(new List<RootBranch>() { new RootBranch(rootChangeSetB2, tfsPathBranch2) });

            newBranch2Remote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT2).Repeat.Once();
            newBranch2Remote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            newBranch2Remote.MaxCommitHash = "ShaAfterFetch_Branch2";
            #endregion

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT3)).Return(GIT_BRANCH_TO_INIT3).Repeat.Never();

            remote.Expect(r => r.Fetch(Arg<bool>.Is.Anything, Arg<int>.Is.Anything, Arg<IRenameResult>.Is.Anything)).Return(new GitTfsRemote.FetchResult() { IsSuccess = true }).Repeat.Once();
            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run());

            gitRepository.VerifyAllExpectations();

            newBranch1Remote.VerifyAllExpectations();
            newBranch2Remote.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldFailInitAllBranchesBecauseNoFetchWasSpecified()
        {
            mocks.ClassUnderTest.CloneAllBranches = true;
            mocks.ClassUnderTest.NoFetch = true;

            var ex = Assert.Throws(typeof(GitTfsException), () => mocks.ClassUnderTest.Run());
            Assert.Equal("error: --no-fetch cannot be used with --all", ex.Message);
        }

        [Fact]
        public void ShouldFailInitAllBranchesBecauseCloneWasNotMadeFromABranch()
        {
            const string GIT_BRANCH_TO_INIT1 = "MyBranch1";
            const string GIT_BRANCH_TO_INIT2 = "MyBranch2";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranch1Remote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT1, out gitRepository, out remote, out newBranch1Remote);

            mocks.ClassUnderTest.CloneAllBranches = true;
            remote.Tfs = mocks.Get<ITfsHelper>();
            var tfsPathBranch1 = "$/MyProject/MyBranch1";
            var tfsPathBranch2 = "$/MyProject/MyBranch2";
            remote.Tfs.Stub(t => t.GetBranches()).Return(new IBranchObject[] {
                new MockBranchObject() { IsRoot = true, Path = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!" },
                new MockBranchObject() { ParentPath = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!", Path = tfsPathBranch1 },
                new MockBranchObject() { ParentPath = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!", Path = tfsPathBranch2 },
                // Note the remote TfsRepositoryPath is NOT included!
            });

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Never();

            #region Branch1
            var rootChangeSetB1 = 1000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch1)).Return(new List<RootBranch>() { new RootBranch(rootChangeSetB1, tfsPathBranch1) });

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT1)).Return(GIT_BRANCH_TO_INIT1).Repeat.Never();
            #endregion

            #region Branch2
            var newBranch2Remote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranch2Remote.Id = GIT_BRANCH_TO_INIT2;

            var rootChangeSetB2 = 2000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch2)).Return(new List<RootBranch>() { new RootBranch(rootChangeSetB2, tfsPathBranch2) });

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT2)).Return(GIT_BRANCH_TO_INIT2).Repeat.Never();
            #endregion

            var ex = Assert.Throws(typeof(GitTfsException), () => mocks.ClassUnderTest.Run());

            Assert.Equal("error: The use of the option '--branches=all' to init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '$/MyProject/Trunk' is not a TFS branch!", ex.Message);

            gitRepository.VerifyAllExpectations();

            newBranch1Remote.VerifyAllExpectations();
            newBranch2Remote.VerifyAllExpectations();
        }

        #endregion

        #region Help Command
        [Fact]
        public void ShouldCallCommandHelp()
        {
            var gitRepository = mocks.Get<IGitRepository>();
            mocks.Get<Globals>().Repository = gitRepository;
            IGitTfsRemote remote = MockRepository.GenerateStub<IGitTfsRemote>();
            remote.TfsUsername = "user";
            remote.TfsPassword = "pwd";
            remote.TfsRepositoryPath = "$/MyProject/Trunk";
            remote.TfsUrl = "http://myTfsServer:8080/tfs";
            remote.Tfs = new TfsHelper(mocks.Container, null);
            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Never();

            //Not Very Clean!!! Don't know how to test that :(
            //If the InvalidOperationException is thrown, it's that the Helper.Run() is Called => That's what is expected!
            Assert.Throws(typeof(InvalidOperationException), () => mocks.ClassUnderTest.Run());
            gitRepository.VerifyAllExpectations();
        }
        #endregion
    }
}
