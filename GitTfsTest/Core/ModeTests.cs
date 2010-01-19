using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Core
{
    [TestClass]
    public class ModeTests
    {
        [TestMethod]
        public void ShouldGetNewFileMode()
        {
            Assert.AreEqual("100644", Mode.NewFile);
        }

        [TestMethod]
        public void ShouldParseBits()
        {
            Assert.AreEqual(16, "20".ToFileMode().Bits);
        }

        [TestMethod]
        public void ShouldFormatFileMode()
        {
            Assert.AreEqual("000020", FileMode.FromBits(16).ToModeString());
        }

        [TestMethod]
        public void ShouldDetectGitLink()
        {
            Assert.AreEqual(FileMode.GitLink, "160000".ToFileMode());
        }

        [TestMethod]
        public void ShouldDetectGitLinkWithEqualityBackwards()
        {
            Assert.AreEqual("160000".ToFileMode(), FileMode.GitLink);
        }
    }
}
