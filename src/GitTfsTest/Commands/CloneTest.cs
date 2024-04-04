using GitTfs.Commands;
using Xunit;

namespace GitTfs.Test.Commands
{
    public class CloneTest : BaseTest
    {
        [Theory]
        [InlineData("-u=login", "--username=xxx")]
        [InlineData("-u login", "--username=xxx")]
        [InlineData("-u  login", "--username=xxx")]
        [InlineData("--username=login", "--username=xxx")]
        [InlineData("--username login", "--username=xxx")]
        [InlineData("--username  login", "--username=xxx")]
        [InlineData("/u=login", "--username=xxx")]
        [InlineData("/u login", "--username=xxx")]
        [InlineData("/u  login", "--username=xxx")]
        [InlineData("/username=login", "--username=xxx")]
        [InlineData("/username login", "--username=xxx")]
        [InlineData("/username  login", "--username=xxx")]

        [InlineData("-p=mypassword", "--password=xxx")]
        [InlineData("-p mypassword", "--password=xxx")]
        [InlineData("-p  mypassword", "--password=xxx")]
        [InlineData("--password=mypassword", "--password=xxx")]
        [InlineData("--password mypassword", "--password=xxx")]
        [InlineData("--password  mypassword", "--password=xxx")]
        [InlineData("/p=mypassword", "--password=xxx")]
        [InlineData("/p mypassword", "--password=xxx")]
        [InlineData("/p  mypassword", "--password=xxx")]
        [InlineData("/password=mypassword", "--password=xxx")]
        [InlineData("/password mypassword", "--password=xxx")]
        [InlineData("/password  mypassword", "--password=xxx")]

        [InlineData("git tfs clone https://tfs/tfs $/repo/branch . --branches=all --username=me --password=ExtraHardPassword",
            "git tfs clone https://tfs/tfs $/repo/branch . --branches=all --username=xxx --password=xxx")]
        [InlineData("git tfs clone https://tfs/tfs $/repo/branch . --username me --password ExtraHardPassword --branches=all",
            "git tfs clone https://tfs/tfs $/repo/branch . --username=xxx --password=xxx --branches=all")]
        [InlineData("git tfs clone --username spraints --password SECRETOMG https://topsecret.com/tfs $/reallysupersecret",
            "git tfs clone --username=xxx --password=xxx https://topsecret.com/tfs $/reallysupersecret")]
        public void ShouldEncodeUserCredentialsInTheCommandLine(string cmd, string output) => Assert.Equal(output, Init.HideUserCredentials(cmd));
    }
}
