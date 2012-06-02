using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Test.Core
{
    [TestClass]
    public class ModeTests
    {
        [TestMethod]
        public void ShouldGetNewFileMode()
        {
            Assert.AreEqual("100644", Sep.Git.Tfs.Core.Mode.NewFile);
        }

        [TestMethod]
        public void ShouldFormatDirectoryFileMode()
        {
            Assert.AreEqual("040000", LibGit2Sharp.Mode.Directory.ToModeString());
        }

        [TestMethod]
        public void ShouldDetectGitLink()
        {
            Assert.AreEqual(LibGit2Sharp.Mode.GitLink, "160000".ToFileMode());
        }

        [TestMethod]
        public void ShouldDetectGitLinkWithEqualityBackwards()
        {
            Assert.AreEqual("160000".ToFileMode(), LibGit2Sharp.Mode.GitLink);
        }
    }
}
