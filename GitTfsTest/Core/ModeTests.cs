﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sep.Git.Tfs.Core;
using LibGit2Sharp;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class ModeTests
    {
        [Fact]
        public void ShouldGetNewFileMode()
        {
            Assert.Equal("100644", Sep.Git.Tfs.Core.Mode.NewFile);
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
