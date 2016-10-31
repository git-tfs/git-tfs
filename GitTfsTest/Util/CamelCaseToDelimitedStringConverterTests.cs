
namespace Sep.Git.Tfs.Test.Util
{
    using Tfs.Util;
    using Xunit;

    public class CamelCaseToDelimitedStringConverterTests
    {
        [Theory]
        [InlineData("Code Reviewer", "-", "code-reviewer")]
        [InlineData(" Code Reviewer", "-", "code-reviewer")]
        [InlineData("Code Reviewer ", "-", "code-reviewer")]
        [InlineData(" Code Reviewer ", "-", "code-reviewer")]
        [InlineData("Code  Reviewer", "-", "code-reviewer")]
        [InlineData("CodeReviewer", "-", "code-reviewer")]
        [InlineData("Jira Issue ID", "-", "jira-issue-id")]
        [InlineData("Jira Issue Id", "-", "jira-issue-id")]
        [InlineData("Some IPAddress", "-", "some-ip-address")]
        [InlineData("SomeIPAddress", "-", "some-ip-address")]
        [InlineData("JustAName", "-", "just-a-name")]
        [InlineData("AnOtherDelimiter", "#", "an#other#delimiter")]
        public void ReturnsExpectedValue(string stringWithCamelCase, string delimiter, string expected)
        {
            var actual = CamelCaseToDelimitedStringConverter.Convert(stringWithCamelCase, delimiter);
            Assert.Equal(expected, actual);
        }
    }
}
