﻿using System;
using System.Linq;
using Sep.Git.Tfs.Core.BranchVisitors;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Test.Commands;
using Xunit;

namespace Sep.Git.Tfs.Test.Core.BranchVisitors
{
    public class BranchContainsPathVisitorTest
    {
        private IBranch branch;

        public BranchContainsPathVisitorTest()
        {
            branch = new InitBranchTest.MockBranch
                {
                    ChildBranches = Enumerable.Empty<InitBranchTest.MockBranch>(),
                    DateCreated = DateTime.Now,
                    Path = @"$/Scratch/Source/Main"
                };
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