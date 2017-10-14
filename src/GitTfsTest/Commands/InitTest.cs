using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using GitTfs.Commands;
using GitTfs.Test.TestHelpers;
using StructureMap.AutoMocking;

namespace GitTfs.Test.Commands
{
    [TestClass]
    public class InitTest
    {
        private StringWriter outputWriter;
        private RhinoAutoMocker<Init> mocks;

        [TestInitialize]
        public void Setup()
        {
            outputWriter = new StringWriter();
            mocks = new RhinoAutoMocker<Init>(MockMode.AAA);
            mocks.Inject<TextWriter>(outputWriter);
        }

        [TestMethod]
        public void ShouldRequireTfsUrl()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldRequireRepositoryPath()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldUseDefaultCredentialsIfUsernameIsNotSpecified()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldPromptForPasswordIfUsernameIsSupplied()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldUseProvidedUsername()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldCreateGitRepositoryWhenItDoesNotExist()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldAddTfsConfigurationToExistingGitRepository()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldAddTfsConfigurationToNewGitRepository()
        {
            Assert.Fail("TODO");
        }

        [TestMethod]
        public void ShouldSetTfsUrlInGitConfig()
        {
            Assert.Fail("TODO");
        }

    }
}
