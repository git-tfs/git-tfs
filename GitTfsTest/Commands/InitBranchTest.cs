using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap.AutoMocking;
using Xunit;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Test.Commands
{
    public class InitBranchTest
    {
        #region Test Init
        private RhinoAutoMocker<InitBranch> mocks;

        public InitBranchTest()
        {
            mocks = new RhinoAutoMocker<InitBranch>();
            mocks.Inject<TextWriter>(new StringWriter());
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
            remote.Tfs = new VsFake.TfsHelper(mocks.Container, null, null);
            gitRepository.Stub(r => r.GitDir).Return(".");

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
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(2010);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Once();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage(Arg<string>.Is.Anything)).Return("sha1BeforeFetch").Repeat.Once();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Return(newBranchRemote).Repeat.Once();

            newBranchRemote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT).Repeat.Once();
            newBranchRemote.Expect(r => r.Fetch()).Repeat.Once();
            newBranchRemote.MaxCommitHash = "sha1AfterFetch";

            gitRepository.Expect(x => x.CreateBranch("refs/remote/tfs/" + GIT_BRANCH_TO_INIT, "sha1BeforeFetch")).Return(true).Repeat.Once();
            gitRepository.Expect(x => x.CreateBranch("refs/heads/" + GIT_BRANCH_TO_INIT, "sha1AfterFetch")).Return(true).Repeat.Once();

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("$/MyProject/MyBranch", expectedGitBranchName));

            gitRepository.VerifyAllExpectations();
            newBranchRemote.VerifyAllExpectations();
        }

        [Fact(Skip = "Forbidden characters are handled, now!")]
        public void ShouldFailIfGitBranchNameIsInvalid()
        {
            const string GIT_BRANCH_TO_INIT = "my~Branch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(2010);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Throw(new GitTfsException("The name specified for the new git branch is not allowed. Choose another one!"));
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage(Arg<string>.Is.Anything)).Return("9ee6a5ab4abd0a96a5e90a6a99988ce59af7964a").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == "myBranch" && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Repeat.Never();

            Assert.Throws(typeof(GitTfsException), ()=>mocks.ClassUnderTest.Run("$/MyProject/MyBranch", GIT_BRANCH_TO_INIT));

            gitRepository.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldDoNothingBecauseRemoteAlreadyExisting()
        {
            const string GIT_BRANCH_TO_INIT = "myBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(2010);

            IGitTfsRemote existingBranchRemote = MockRepository.GenerateStub<IGitTfsRemote>();
            existingBranchRemote.TfsUsername = "user";
            existingBranchRemote.TfsPassword = "pwd";
            existingBranchRemote.TfsRepositoryPath = "$/MyProject/MyBranch";
            existingBranchRemote.TfsUrl = "http://myTfsServer:8080/tfs";
            existingBranchRemote.Tfs = new VsFake.TfsHelper(mocks.Container, null, null);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote, existingBranchRemote }).Repeat.Once();

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Never();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage(Arg<string>.Is.Anything)).Return("9ee6a5ab4abd0a96a5e90a6a99988ce59af7964a").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == "myBranch" && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Repeat.Never();

            Assert.Equal(GitTfsExitCodes.InvalidArguments, mocks.ClassUnderTest.Run("$/MyProject/MyBranch", GIT_BRANCH_TO_INIT));

            gitRepository.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldFailBecauseRootChangeSetNotFound()
        {
            const string GIT_BRANCH_TO_INIT = "MyBranch";

            IGitRepository gitRepository; IGitTfsRemote remote; IGitTfsRemote newBranchRemote;
            InitMocks4Tests(GIT_BRANCH_TO_INIT, out gitRepository, out remote, out newBranchRemote);

            remote.Tfs = mocks.Get<ITfsHelper>();
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(-1);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Once();
            gitRepository.Expect(x => x.CommandOneline(Arg<string[]>.Is.Anything)).Return("foo!").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Repeat.Never();
            gitRepository.Expect(x => x.ReadTfsRemote(GIT_BRANCH_TO_INIT)).Return(newBranchRemote).Repeat.Never();

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
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(2010);

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Once();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage(Arg<string>.Is.Anything)).Return("").Repeat.Once();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Repeat.Never();
            gitRepository.Expect(x => x.ReadTfsRemote(GIT_BRANCH_TO_INIT)).Return(newBranchRemote).Repeat.Never();


            Assert.Throws(typeof(GitTfsException), ()=>mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));

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
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch", remote.TfsRepositoryPath)).Return(2008);

            mocks.ClassUnderTest.ParentBranch = remote.TfsRepositoryPath;

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Once();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage(Arg<string>.Is.Anything)).Return("sha1BeforeFetch").Repeat.Once();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Return(newBranchRemote).Repeat.Once();

            newBranchRemote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT).Repeat.Once();
            newBranchRemote.Expect(r => r.Fetch()).Repeat.Once();
            newBranchRemote.MaxCommitHash = "sha1AfterFetch";

            gitRepository.Expect(x => x.CreateBranch("refs/remote/tfs/" + GIT_BRANCH_TO_INIT, "sha1BeforeFetch")).Return(true).Repeat.Once();
            gitRepository.Expect(x => x.CreateBranch("refs/heads/" + GIT_BRANCH_TO_INIT, "sha1AfterFetch")).Return(true).Repeat.Once();

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
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch("$/MyProject/MyBranch")).Return(2008);

            mocks.ClassUnderTest.ParentBranch = "$/MyProject/MyParentBranchNotAlreadyCloned";

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT)).Return(GIT_BRANCH_TO_INIT).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage(Arg<string>.Is.Anything)).Return("9ee6a5ab4abd0a96a5e90a6a99988ce59af7964a").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == "$/MyProject/MyBranch").Repeat.Never();
            gitRepository.Expect(x => x.ReadTfsRemote(GIT_BRANCH_TO_INIT)).Return(newBranchRemote).Repeat.Never();

            Assert.Throws(typeof(GitTfsException), ()=>mocks.ClassUnderTest.Run("$/MyProject/MyBranch"));

            gitRepository.VerifyAllExpectations();
        }
        #endregion

        #region Init All branches

        public class MockBranchObject : IBranchObject
        {
            public string Path { get; set; }

            public string ParentPath { get; set; }

            public bool IsRoot { get; set; }
        }

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
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();

            #region Branch1
            var rootChangeSetB1 = 1000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch1)).Return(rootChangeSetB1);

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT1)).Return(GIT_BRANCH_TO_INIT1).Repeat.Once();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetB1 + "[^0-9]")).Return("ShaBeforeFetch_Branch1").Repeat.Once();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT1 && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == tfsPathBranch1).Return(newBranch1Remote).Repeat.Once();

            newBranch1Remote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT1).Repeat.Once();
            newBranch1Remote.Expect(r => r.Fetch()).Repeat.Once();
            newBranch1Remote.MaxCommitHash = "ShaAfterFetch_Branch1";

            gitRepository.Expect(x => x.CreateBranch("refs/remote/tfs/" + GIT_BRANCH_TO_INIT1, "ShaBeforeFetch_Branch1")).Return(true).Repeat.Once();
            gitRepository.Expect(x => x.CreateBranch("refs/heads/" + GIT_BRANCH_TO_INIT1, "ShaAfterFetch_Branch1")).Return(true).Repeat.Once();
            #endregion

            #region Branch2
            var newBranch2Remote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranch2Remote.Id = GIT_BRANCH_TO_INIT2;

            var rootChangeSetB2 = 2000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch2)).Return(rootChangeSetB2);

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT2)).Return(GIT_BRANCH_TO_INIT2).Repeat.Once();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetB2 + "[^0-9]")).Return("ShaBeforeFetch_Branch2").Repeat.Once();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT2 && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == tfsPathBranch2).Return(newBranch2Remote).Repeat.Once();

            newBranch2Remote.Expect(r => r.RemoteRef).Return("refs/remote/tfs/" + GIT_BRANCH_TO_INIT2).Repeat.Once();
            newBranch2Remote.Expect(r => r.Fetch()).Repeat.Once();
            newBranch2Remote.MaxCommitHash = "ShaAfterFetch_Branch2";

            gitRepository.Expect(x => x.CreateBranch("refs/remote/tfs/" + GIT_BRANCH_TO_INIT2, "ShaBeforeFetch_Branch2")).Return(true).Repeat.Once();
            gitRepository.Expect(x => x.CreateBranch("refs/heads/" + GIT_BRANCH_TO_INIT2, "ShaAfterFetch_Branch2")).Return(true).Repeat.Once();
            #endregion

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run());

            gitRepository.VerifyAllExpectations();
            newBranch1Remote.VerifyAllExpectations();
            newBranch2Remote.VerifyAllExpectations();
        }

        [Fact]
        public void ShouldFailInitAllBranchesBecauseNeedCloneWasMadeFromTrunk()
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
                new MockBranchObject() { ParentPath = "$/MyProject/TheCloneWasNotMadeFromTheTrunk!", Path = remote.TfsRepositoryPath },
            });

            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Once();
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();

            #region Branch1
            var rootChangeSetB1 = 1000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch1)).Return(rootChangeSetB1);

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT1)).Return(GIT_BRANCH_TO_INIT1).Repeat.Never();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetB1 + "[^0-9]")).Return("Sha_Branch1").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT1 && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == tfsPathBranch1).Repeat.Never();
            #endregion

            #region Branch2
            var newBranch2Remote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranch2Remote.Id = GIT_BRANCH_TO_INIT2;

            var rootChangeSetB2 = 2000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch2)).Return(rootChangeSetB2);

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT2)).Return(GIT_BRANCH_TO_INIT2).Repeat.Never();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetB2 + "[^0-9]")).Return("Sha_Branch2").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT2 && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == tfsPathBranch2).Repeat.Never();
            #endregion

            var ex = Assert.Throws(typeof(GitTfsException), ()=>mocks.ClassUnderTest.Run());

            Assert.Equal("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! Please clone again from '$/MyProject/TheCloneWasNotMadeFromTheTrunk!'...", ex.Message);

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
            Assert.Equal("error: --nofetch cannot be used with --all", ex.Message);
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
            gitRepository.Expect(x => x.ReadAllTfsRemotes()).Return(new List<IGitTfsRemote> { remote }).Repeat.Once();

            #region Branch1
            var rootChangeSetB1 = 1000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch1)).Return(rootChangeSetB1);

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT1)).Return(GIT_BRANCH_TO_INIT1).Repeat.Never();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetB1 + "[^0-9]")).Return("Sha_Branch1").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT1 && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == tfsPathBranch1).Repeat.Never();
            gitRepository.Expect(x => x.ReadTfsRemote(GIT_BRANCH_TO_INIT1)).Return(newBranch1Remote).Repeat.Never();
            #endregion

            #region Branch2
            var newBranch2Remote = MockRepository.GenerateStub<IGitTfsRemote>();
            newBranch2Remote.Id = GIT_BRANCH_TO_INIT2;

            var rootChangeSetB2 = 2000;
            remote.Tfs.Stub(t => t.GetRootChangesetForBranch(tfsPathBranch2)).Return(rootChangeSetB2);

            gitRepository.Expect(x => x.AssertValidBranchName(GIT_BRANCH_TO_INIT2)).Return(GIT_BRANCH_TO_INIT2).Repeat.Never();
            gitRepository.Expect(x => x.FindCommitHashByCommitMessage("git-tfs-id: .*;C" + rootChangeSetB2 + "[^0-9]")).Return("Sha_Branch2").Repeat.Never();
            gitRepository.Expect(x => x.CreateTfsRemote(null)).Callback<RemoteInfo>((info) => info.Id == GIT_BRANCH_TO_INIT2 && info.Url == "http://myTfsServer:8080/tfs" && info.Repository == tfsPathBranch2).Repeat.Never();
            gitRepository.Expect(x => x.ReadTfsRemote(GIT_BRANCH_TO_INIT2)).Return(newBranch2Remote).Repeat.Never();
            #endregion

            var ex = Assert.Throws(typeof(GitTfsException), () => mocks.ClassUnderTest.Run());

            Assert.Equal("error: Init all the branches is only possible when 'git tfs clone' was done from the trunk!!! '$/MyProject/Trunk' is not a TFS branch!", ex.Message);

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
            remote.Tfs = new VsFake.TfsHelper(mocks.Container, null, null);
            gitRepository.Expect(x => x.ReadTfsRemote("default")).Return(remote).Repeat.Never();

            //Not Very Clean!!! Don't know how to test that :(
            //If the InvalidOperationException is thrown, it's that the Helper.Run() is Called => That's what is expected!
            Assert.Throws(typeof(InvalidOperationException), () => mocks.ClassUnderTest.Run());
            gitRepository.VerifyAllExpectations();
        }
        #endregion

        #region
        [Fact]
        public void ShouldHaveGoodName()
        {
           Globals globals = new Globals(null);
           globals.Repository = mocks.Get<IGitRepository>();
           globals.Repository.Stub(t => t.AssertValidBranchName("")).IgnoreArguments().Do ( (Func<string, string>) delegate (string value) { return value; });

           var initBranch4test = new InitBranch4Test(new StringWriter(), globals , null, null);
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("test"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te^st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te~st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te:st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te*st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te?st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te[st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("test/"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("test."));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("$/repo/te:st"));
           Assert.Equal("test/test2", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("$/repo/te:st/test2"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te..st"));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("test."));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te\\st."));
           Assert.Equal("test", initBranch4test.ExtractGitBranchNameFromTfsRepositoryPath("te@{st."));
        }

        public class InitBranch4Test : InitBranch
        {
            public InitBranch4Test(TextWriter stdout, Globals globals, Help helper, AuthorsFile authors) : base(stdout, globals, helper, authors) { }

            public string ExtractGitBranchNameFromTfsRepositoryPath(string tfsRepositoryPath)
            {
                return base.ExtractGitBranchNameFromTfsRepositoryPath(tfsRepositoryPath);
            }
        }
        #endregion
    }
}
