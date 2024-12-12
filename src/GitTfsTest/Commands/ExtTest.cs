using GitTfs.Commands;
using GitTfs.Core;
using Xunit;

namespace GitTfs.Test.Commands
{
    public class ExtTest : BaseTest
    {
        [Fact]
        public void AssertValidTfsPathTest()
        {
            "$/test".AssertValidTfsPath();
            Assert.Throws<GitTfsException>(() => "$test".AssertValidTfsPath());
            Assert.Throws<GitTfsException>(() => "/test".AssertValidTfsPath());
            Assert.Throws<GitTfsException>(() => "test".AssertValidTfsPath());
            Assert.Throws<GitTfsException>(() => "$/".AssertValidTfsPath());
            "$/".AssertValidTfsPathOrRoot();
        }

        [Fact]
        public void ToGitRefNameTest()
        {
            Assert.Equal("test", "test".ToGitRefName());
            Assert.Equal("test", "te^st".ToGitRefName());
            Assert.Equal("test", "te~st".ToGitRefName());
            Assert.Equal("test", "te st".ToGitRefName());
            Assert.Equal("test", "te:st".ToGitRefName());
            Assert.Equal("test", "te*st".ToGitRefName());
            Assert.Equal("test", "te?st".ToGitRefName());
            Assert.Equal("test", "te[st".ToGitRefName());
            Assert.Equal("test", "test/".ToGitRefName());
            Assert.Equal("test", "test.".ToGitRefName());
            Assert.Equal("test", "te..st".ToGitRefName());
            Assert.Equal("test", "test.".ToGitRefName());
            Assert.Equal("test", "te\\st.".ToGitRefName());
            Assert.Equal("test", "te@{st.".ToGitRefName());
            Assert.Equal("test", "/test././".ToGitRefName());
            Assert.Equal("test", ".test".ToGitRefName());
            Assert.Equal("bugs/nameOfTheBug", "/bu$gs/name:OfTheBug".ToGitRefName());
            Assert.Equal("repo/test/test2", "$/repo/te:st/test2".ToGitRefName());
        }

        [Fact]
        public void GetAGitBranchNameFromTfsRepositoryPath()
        {
            Assert.Equal("test", "test".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te^st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te~st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te:st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te*st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te?st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te[st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "test/".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "test.".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "$/repo/te:st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test/test2", "$/repo/te:st/test2".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te..st".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "test.".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te\\st.".ToGitBranchNameFromTfsRepositoryPath());
            Assert.Equal("test", "te@{st.".ToGitBranchNameFromTfsRepositoryPath());
        }
    }
}
