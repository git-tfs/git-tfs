using System.IO;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using StructureMap.AutoMocking;
using Xunit;
using Xunit.Extensions;

namespace Sep.Git.Tfs.Test.Commands
{
    public class CloneTest
    {
        [Theory]
        [InlineData("-u=login", "--username=xxx")]
        [InlineData("-u login", "--username=xxx")]
        [InlineData("--username=login", "--username=xxx")]
        [InlineData("--username login", "--username=xxx")]

        [InlineData("-p=mypassword", "--password=xxx")]
        [InlineData("-p mypassword", "--password=xxx")]
        [InlineData("--password=mypassword", "--password=xxx")]
        [InlineData("--password mypassword", "--password=xxx")]

        [InlineData("git tfs clone https://tfs/tfs $/repo/branch . --with-branches --username=me --password=ExtraHardPassword",
            "git tfs clone https://tfs/tfs $/repo/branch . --with-branches --username=xxx --password=xxx")]
        [InlineData("git tfs clone https://tfs/tfs $/repo/branch . --username me --password ExtraHardPassword --with-branches",
            "git tfs clone https://tfs/tfs $/repo/branch . --username=xxx --password=xxx --with-branches")]
        [InlineData("git tfs clone --username spraints --password SECRETOMG https://topsecret.com/tfs $/reallysupersecret",
            "git tfs clone --username=xxx --password=xxx https://topsecret.com/tfs $/reallysupersecret")]
        public void ShouldEncodeUserCredentialsInTheCommandLine(string cmd, string output)
        {
            Assert.Equal(output, Init.HideUserCredentials(cmd));
        }
    }
}
