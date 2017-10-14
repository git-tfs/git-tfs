using Xunit;

namespace Sep.Git.Tfs.Test
{
    public class GitTfsRegexTests : BaseTest
    {
        [Fact]
        public void CommitRegexShouldApproveGitCommitTitle()
        {
            const string line = "commit 9b655abe865ef0e4048aba904b79c7a2f10bdfce";
            var match = GitTfsConstants.CommitRegex.Match(line);
            Assert.True(match.Success);
        }

        [Fact]
        public void CommitRegexShouldDeclineCommitRevertMessage()
        {
            const string line = "    This reverts commit e096daaf57d937fef3c0c639c3a59232310c6a20.";
            var match = GitTfsConstants.CommitRegex.Match(line);
            Assert.False(match.Success);
        }
    }
}
