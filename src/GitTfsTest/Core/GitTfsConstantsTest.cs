using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class GitTfsConstantsTest : BaseTest
    {
        [Fact]
        public void TestTfsCommitInfoRegex_WhenTheRepositoryContainsSquareBrackets_ThenWeGetTheCorrectValues()
        {
            string url = "http://tfsserver:8080/tfs/MainProjectCollection";
            string repository = "$/Server/Branches/V1.1/Features/v1.1 [Bugs]/ShopServer";
            string changesetId = "177712";

            string gitTfsMetaInfo = "git-tfs-id: [" + url + "]" + repository + ";C" + changesetId;

            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);

            Assert.Equal(url, match.Groups["url"].Value);
            Assert.Equal(repository, match.Groups["repository"].Value);
            Assert.Equal(changesetId, match.Groups["changeset"].Value);
        }
    }
}
