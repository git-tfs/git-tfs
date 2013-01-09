using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap.AutoMocking;
using Xunit;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Test.Commands
{
    public class ExtTest
    {
        [Fact]
        public void AssertValidTfsPathTest()
        {
            Assert.DoesNotThrow(() => "$/test".AssertValidTfsPath());
            Assert.Throws(typeof(GitTfsException), () => "$test".AssertValidTfsPath());
            Assert.Throws(typeof(GitTfsException), () => "/test".AssertValidTfsPath());
            Assert.Throws(typeof(GitTfsException), () => "test".AssertValidTfsPath());
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
            Assert.Equal("bugs/nameOfTheBug", "/bu$gs/name:OfTheBug".ToGitRefName());
            Assert.Equal("repo/test/test2", "$/repo/te:st/test2".ToGitRefName());
        }
    }
}
