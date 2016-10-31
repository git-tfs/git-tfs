using Sep.Git.Tfs.Core.BranchVisitors;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.VsFake;
using Xunit;

namespace Sep.Git.Tfs.Test.Core.BranchVisitors
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