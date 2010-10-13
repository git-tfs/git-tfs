using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;
using GitSharp.Core;

namespace Sep.Git.Tfs.Test.Core
{
    [TestClass]
    public class GitChangeInfoTests
    {
        [TestMethod]
        public void GetsMode()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("100644", info.NewMode.ToModeString());
        }

        [TestMethod]
        public void GetsLinkMode()
        {
            var line = ":000000 160000 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual(FileMode.GitLink, info.NewMode);
        }

        [TestMethod]
        public void GetsChangeType()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("M", info.Status);
        }

        [TestMethod]
        public void GetsChangeTypeWhenScoreIsPresent()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("R", info.Status);
        }
    }
}
