using Sep.Git.Tfs.Core;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class ModeTests : BaseTest
    {
        [Fact]
        public void ShouldGetNewFileMode()
        {
            Assert.Equal("100644", Mode.NewFile);
        }

        [Fact]
        public void ShouldFormatDirectoryFileMode()
        {
            Assert.Equal("040000", LibGit2Sharp.Mode.Directory.ToModeString());
        }

        [Fact]
        public void ShouldDetectGitLink()
        {
            Assert.Equal(LibGit2Sharp.Mode.GitLink, "160000".ToFileMode());
        }

        [Fact]
        public void ShouldDetectGitLinkWithEqualityBackwards()
        {
            Assert.Equal("160000".ToFileMode(), LibGit2Sharp.Mode.GitLink);
        }
    }
}
