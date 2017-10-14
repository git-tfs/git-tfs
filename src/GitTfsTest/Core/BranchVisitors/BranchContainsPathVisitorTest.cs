﻿using GitTfs.Core.BranchVisitors;
using GitTfs.Core.TfsInterop;
using GitTfs.VsFake;
using Xunit;

namespace GitTfs.Test.Core.BranchVisitors
{
    public class BranchContainsPathVisitorTest : BaseTest
    {
        private readonly BranchTree branch;

        public BranchContainsPathVisitorTest()
        {
            branch = new BranchTree(new MockBranchObject { Path = @"$/Scratch/Source/Main" });
        }

        [Fact]
        public void InexactMatch_WithoutTrailingSlash_IsFound()
        {
            var visitor = new BranchTreeContainsPathVisitor(@"$/Scratch/Source/Main", false);

            branch.AcceptVisitor(visitor);

            Assert.True(visitor.Found);
        }

        [Fact]
        public void InexactMatch_WithTrailingSlash_IsFound()
        {
            var visitor = new BranchTreeContainsPathVisitor(@"$/Scratch/Source/Main/", false);

            branch.AcceptVisitor(visitor);

            Assert.True(visitor.Found);
        }

        [Fact]
        public void ExactMatch_WithoutTrailingSlash_IsFound()
        {
            var visitor = new BranchTreeContainsPathVisitor(@"$/Scratch/Source/Main", true);

            branch.AcceptVisitor(visitor);

            Assert.True(visitor.Found);
        }

        [Fact]
        public void ExactMatch_WithTrailingSlash_IsNotFound()
        {
            var visitor = new BranchTreeContainsPathVisitor(@"$/Scratch/Source/Main/", true);

            branch.AcceptVisitor(visitor);

            Assert.False(visitor.Found);
        }
    }
}