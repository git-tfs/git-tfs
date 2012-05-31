using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.Changes.Git;
using StructureMap;
using LibGit2Sharp;

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
            Assert.AreEqual(LibGit2Sharp.Mode.GitLink, info.NewMode);
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

        [TestMethod]
        public void GetsPath()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tFoo\tBar";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("Foo", info.path);
        }

        [TestMethod]
        public void GetsPathTo()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tFoo\tBar";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("Bar", info.pathTo);
        }

        [TestMethod]
        public void GetsPathWithQuotepath()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\t\"\\366\"\t\"\\337\"";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("ö", info.path);
        }

        [TestMethod]
        public void GetsPathToWithQuotepath()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\t\"\\366\"\t\"\\337\"";
            var info = GitChangeInfo.Parse(line);
            Assert.AreEqual("ß", info.pathTo);
        }

        private IGitChangedFile GetChangeItem(string diffTreeLine)
        {
            // This method is similar to BuildGitChangedFile in GitRepository.
            var container = new Container(x => { Program.AddGitChangeTypes(x); });
            return GitChangeInfo.Parse(diffTreeLine).ToGitChangedFile(container.With((IGitRepository)null));
        }

        [TestMethod]
        public void GetsInstanceOfAdd()
        {
            var change = GetChangeItem(":000000 100644 0000000000000000000000000000000000000000 01234567ab01234567ab01234567ab01234567ab A\tblah");
            Assert.IsInstanceOfType(change, typeof(Add));
        }

        [TestMethod]
        public void GetsInstanceOfAddWithNewMode()
        {
            var change = (Add)GetChangeItem(":000000 100644 0000000000000000000000000000000000000000 01234567ab01234567ab01234567ab01234567ab A\tblah");
            Assert.AreEqual("01234567ab01234567ab01234567ab01234567ab", change.NewSha);
        }

        [TestMethod]
        public void GetsInstanceOfAddWithNewPath()
        {
            var change = (Add)GetChangeItem(":000000 100644 0000000000000000000000000000000000000000 01234567ab01234567ab01234567ab01234567ab A\tblah");
            Assert.AreEqual("blah", change.Path);
        }

        [TestMethod]
        public void GetsInstanceOfCopy()
        {
            var change = GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab C100\toldname\tnewname");
            Assert.IsInstanceOfType(change, typeof(Copy));
        }

        [TestMethod]
        public void GetsInstanceOfAddForCopyWithPath()
        {
            var change = (Copy)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab C100\toldname\tnewname");
            Assert.AreEqual("newname", change.Path);
        }

        [TestMethod]
        public void GetsInstanceOfModify()
        {
            var change = GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah");
            Assert.IsInstanceOfType(change, typeof(Modify));
        }

        [TestMethod]
        public void GetsInstanceOfModifyWithPath()
        {
            var change = (Modify)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah");
            Assert.AreEqual("blah", change.Path);
        }

        [TestMethod]
        public void GetsInstanceOfModifyWithNewSha()
        {
            var change = (Modify)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah");
            Assert.AreEqual("01234567ab01234567ab01234567ab01234567ab", change.NewSha);
        }

        [TestMethod]
        public void GetsInstanceOfDelete()
        {
            var change = GetChangeItem(":100644 000000 abcdef0123abcdef0123abcdef0123abcdef0123 0000000000000000000000000000000000000000 D\tblah");
            Assert.IsInstanceOfType(change, typeof(Delete));
        }

        [TestMethod]
        public void GetsInstanceOfDeleteWithPath()
        {
            var change = (Delete)GetChangeItem(":100644 000000 abcdef0123abcdef0123abcdef0123abcdef0123 0000000000000000000000000000000000000000 D\tblah");
            Assert.AreEqual("blah", change.Path);
        }

        [TestMethod]
        public void GetsInstanceOfRenameEdit()
        {
            var change = GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.IsInstanceOfType(change, typeof(RenameEdit));
        }

        [TestMethod]
        public void GetsInstanceOfRenameEditWithPath()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.AreEqual("blah", change.Path);
        }

        [TestMethod]
        public void GetsInstanceOfRenameEditWithPathTo()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.AreEqual("newblah", change.PathTo);
        }

        [TestMethod]
        public void GetsInstanceOfRenameEditWithNewSha()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.AreEqual("01234567ab01234567ab01234567ab01234567ab", change.NewSha);
        }

        [TestMethod]
        public void GetsInstanceOfRenameEditWithScore()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.AreEqual("001", change.Score);
        }
    }
}
