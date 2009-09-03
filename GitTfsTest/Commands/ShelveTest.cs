using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Test.TestHelpers;
using StructureMap.AutoMocking;

namespace Sep.Git.Tfs.Test.Commands
{
    [TestClass]
    public class ShelveTest
    {
        private RhinoAutoMocker<Shelve> mocks;

        [TestInitialize]
        public void Setup()
        {
            mocks = new RhinoAutoMocker<Shelve>();
            mocks.Inject<TextWriter>(new StringWriter());
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
            mocks.MockObjectFactory();
        }

        [TestMethod]
        public void ShouldFailForZeroArguments()
        {
            this.FixHelpFormatter(
                () => Assert.AreNotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.MakeArgsAndRun()));
        }

        [TestMethod]
        public void ShouldFailForThreeArguments()
        {
            this.FixHelpFormatter(
                () => Assert.AreNotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.MakeArgsAndRun("one", "two", "three")));
        }

        [TestMethod]
        public void ShouldSucceedForOneArgument()
        {
            mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(mocks.Get<IGitTfsRemote>());

            Assert.AreEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.MakeArgsAndRun("don't care"));
        }

        [TestMethod]
        public void ShouldUseRepositoryConfiguredOnTheCommandLine()
        {
            // The command line argument will end up in globals...
            mocks.Get<Globals>().RemoteId = "remote-id";
            mocks.Get<IGitRepository>().Expect(x => x.ReadTfsRemote("remote-id")).Return(mocks.Get<IGitTfsRemote>());

            mocks.ClassUnderTest.MakeArgsAndRun("don't care");
        }

        [TestMethod]
        public void ShouldTellRemoteToShelve()
        {
            var remote = mocks.Get<IGitTfsRemote>();
            mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(remote);

            mocks.ClassUnderTest.MakeArgsAndRun("shelveset name");

            remote.AssertWasCalled(x => x.Shelve("shelveset name", "HEAD"));
        }

        [TestMethod]
        public void ShouldTellRemoteToShelveTreeish()
        {
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
            var remote = mocks.Get<IGitTfsRemote>();
            mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(remote);

            mocks.ClassUnderTest.MakeArgsAndRun("shelveset name", "treeish");

            remote.AssertWasCalled(x => x.Shelve("shelveset name", "treeish"));
        }
    }
}
