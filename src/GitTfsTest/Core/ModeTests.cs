using GitTfs.Core;
using Xunit;

namespace GitTfs.Test.Core
{
    public class ModeTests : BaseTest
    {
        [Fact]
        public void ShouldGetNewFileMode() => Assert.Equal("100644", Mode.NewFile);

        [Fact]
        public void ShouldFormatDirectoryFileMode() => Assert.Equal("040000", LibGit2Sharp.Mode.Directory.ToModeString());

        [Fact]
        public void ShouldDetectGitLink() => Assert.Equal(LibGit2Sharp.Mode.GitLink, "160000".ToFileMode());

        [Fact]
        public void ShouldDetectGitLinkWithEqualityBackwards() =>
#pragma warning disable xUnit2000 // Expected value should be first
            Assert.Equal("160000".ToFileMode(), actual: LibGit2Sharp.Mode.GitLink);
#pragma warning restore xUnit2000 // Expected value should be first

    }
}
