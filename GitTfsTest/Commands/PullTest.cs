using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using StructureMap.AutoMocking;

namespace Sep.Git.Tfs.Test.Commands
{
    [TestClass()]
    public class PullTest
    {
        private RhinoAutoMocker<Pull> mocks;

        [TestInitialize]
        public void Setup()
        {
            mocks = new RhinoAutoMocker<Pull>();
            mocks.Inject<TextWriter>(new StringWriter());
        }

        [TestMethod(), ExpectedException(typeof(GitTfsException))]
        public void ShouldFailIfLocalChangesExist()
        {
            const string remoteRefId = "my remote ref";

            var mockTfsRemote = mocks.Get<IGitTfsRemote>();
            mockTfsRemote.Stub(x => x.RemoteRef).Return(remoteRefId);

            var mockRepository = mocks.Get<IGitRepository>();
            mockRepository.Stub(x => x.ReadTfsRemote("")).IgnoreArguments().Return(mocks.Get<IGitTfsRemote>());
            mockRepository.Stub(x => x.ReadAllTfsRemotes()).Return(new IGitTfsRemote[] { mockTfsRemote });
            mockRepository.Stub(x => x.WorkingCopyHasUnstagedOrUncommitedChanges).Return(true);

            var mockGlobals = mocks.Get<Globals>();
            mockGlobals.Repository = mockRepository;

            mockRepository.AssertWasNotCalled(x => x.CommandNoisy("merge", remoteRefId));

            mocks.ClassUnderTest.Run();

            mockRepository.VerifyAllExpectations();
        }
    }
}
