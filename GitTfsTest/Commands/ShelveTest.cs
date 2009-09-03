using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Test.TestHelpers;
using StructureMap;
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
            mocks.MockObjectFactory();
            mocks.Inject<TextWriter>(new StringWriter());
        }

        [TestMethod]
        public void ShouldFailForZeroArguments()
        {
            this.WithFixForHelpColumnWidth(
                () => Assert.AreNotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.MakeArgsAndRun()));
        }
    }
}
