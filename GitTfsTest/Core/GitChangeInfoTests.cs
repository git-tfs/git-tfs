using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.Changes.Git;
using StructureMap;
using LibGit2Sharp;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class GitChangeInfoTests
    {
        [Fact]
        public void GetsMode()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("100644", info.NewMode.ToModeString());
        }

        [Fact]
        public void GetsLinkMode()
        {
            var line = ":000000 160000 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal(LibGit2Sharp.Mode.GitLink, info.NewMode);
        }

        [Fact]
        public void GetsChangeType()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("M", info.Status);
        }

        [Fact]
        public void GetsChangeTypeWhenScoreIsPresent()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("R", info.Status);
        }

        [Fact]
        public void GetsPath()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tFoo\tBar";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("Foo", info.path);
        }

        [Fact]
        public void GetsPathTo()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tFoo\tBar";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("Bar", info.pathTo);
        }

        [Fact]
        public void GetsPathWithQuotepath()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\t\"\\366\"\t\"\\337\"";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("ö", info.path);
        }

        [Fact]
        public void GetsPathToWithQuotepath()
        {
            var line = ":000000 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\t\"\\366\"\t\"\\337\"";
            var info = GitChangeInfo.Parse(line);
            Assert.Equal("ß", info.pathTo);
        }

        private IGitChangedFile GetChangeItem(string diffTreeLine)
        {
            // This method is similar to BuildGitChangedFile in GitRepository.
            var container = new Container(x => { Program.AddGitChangeTypes(x); });
            return GitChangeInfo.Parse(diffTreeLine).ToGitChangedFile(container.With((IGitRepository)null));
        }

        [Fact]
        public void GetsInstanceOfAdd()
        {
            var change = GetChangeItem(":000000 100644 0000000000000000000000000000000000000000 01234567ab01234567ab01234567ab01234567ab A\tblah");
            Assert.IsType<Add>(change);
        }

        [Fact]
        public void GetsInstanceOfAddWithNewMode()
        {
            var change = (Add)GetChangeItem(":000000 100644 0000000000000000000000000000000000000000 01234567ab01234567ab01234567ab01234567ab A\tblah");
            Assert.Equal("01234567ab01234567ab01234567ab01234567ab", change.NewSha);
        }

        [Fact]
        public void GetsInstanceOfAddWithNewPath()
        {
            var change = (Add)GetChangeItem(":000000 100644 0000000000000000000000000000000000000000 01234567ab01234567ab01234567ab01234567ab A\tblah");
            Assert.Equal("blah", change.Path);
        }

        [Fact]
        public void GetsInstanceOfCopy()
        {
            var change = GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab C100\toldname\tnewname");
            Assert.IsType<Copy>(change);
        }

        [Fact]
        public void GetsInstanceOfAddForCopyWithPath()
        {
            var change = (Copy)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab C100\toldname\tnewname");
            Assert.Equal("newname", change.Path);
        }

        [Fact]
        public void GetsInstanceOfModify()
        {
            var change = GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah");
            Assert.IsType<Modify>(change);
        }

        [Fact]
        public void GetsInstanceOfModifyWithPath()
        {
            var change = (Modify)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah");
            Assert.Equal("blah", change.Path);
        }

        [Fact]
        public void GetsInstanceOfModifyWithNewSha()
        {
            var change = (Modify)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab M\tblah");
            Assert.Equal("01234567ab01234567ab01234567ab01234567ab", change.NewSha);
        }

        [Fact]
        public void GetsInstanceOfDelete()
        {
            var change = GetChangeItem(":100644 000000 abcdef0123abcdef0123abcdef0123abcdef0123 0000000000000000000000000000000000000000 D\tblah");
            Assert.IsType<Delete>(change);
        }

        [Fact]
        public void GetsInstanceOfDeleteWithPath()
        {
            var change = (Delete)GetChangeItem(":100644 000000 abcdef0123abcdef0123abcdef0123abcdef0123 0000000000000000000000000000000000000000 D\tblah");
            Assert.Equal("blah", change.Path);
        }

        [Fact]
        public void GetsInstanceOfRenameEdit()
        {
            var change = GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.IsType<RenameEdit>(change);
        }

        [Fact]
        public void GetsInstanceOfRenameEditWithPath()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.Equal("blah", change.Path);
        }

        [Fact]
        public void GetsInstanceOfRenameEditWithPathTo()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.Equal("newblah", change.PathTo);
        }

        [Fact]
        public void GetsInstanceOfRenameEditWithNewSha()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.Equal("01234567ab01234567ab01234567ab01234567ab", change.NewSha);
        }

        [Fact]
        public void GetsInstanceOfRenameEditWithScore()
        {
            var change = (RenameEdit)GetChangeItem(":100644 100644 abcdef0123abcdef0123abcdef0123abcdef0123 01234567ab01234567ab01234567ab01234567ab R001\tblah\tnewblah");
            Assert.Equal("001", change.Score);
        }
    }
}
