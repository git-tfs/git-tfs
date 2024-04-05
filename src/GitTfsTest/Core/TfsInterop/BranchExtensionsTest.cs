using GitTfs.Core.TfsInterop;

using Moq;

using Xunit;

namespace GitTfs.Test.Core.TfsInterop
{
    public class BranchExtensionsTest : BaseTest
    {
        [Fact]
        public void AllChildrenAlwaysReturnsAnEnumerable()
        {
            IEnumerable<BranchTree> result = ((BranchTree)null).GetAllChildren();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        private BranchTree CreateBranchTree(string tfsPath, BranchTree parent = null)
        {
            var branchObject = new Mock<IBranchObject>();
            branchObject.Setup(m => m.Path).Returns(tfsPath);

            var branchTree = new BranchTree(branchObject.Object);

            if (parent != null)
                parent.ChildBranches.Add(branchTree);
            return branchTree;
        }

        [Fact]
        public void WhenGettingChildrenOfTopBranch_ThenReturnAllTheChildren()
        {
            var trunk = CreateBranchTree("$/Project/Trunk");

            var branch1 = CreateBranchTree("$/Project/Branch1", trunk);
            var branch2 = CreateBranchTree("$/Project/Branch2", trunk);
            var branch3 = CreateBranchTree("$/Project/Branch3", trunk);

            IEnumerable<BranchTree> result = trunk.GetAllChildren();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            Assert.Equal(new List<BranchTree> { branch1, branch2, branch3 }, result);
        }

        [Fact]
        public void WhenGettingChildrenOfOneBranch_ThenReturnChildrenOfThisBranch()
        {
            var trunk = CreateBranchTree("$/Project/Trunk");

            var branch1 = CreateBranchTree("$/Project/Branch1", trunk);
            var branch1_1 = CreateBranchTree("$/Project/Branch1.1", branch1);
            var branch1_2 = CreateBranchTree("$/Project/Branch1.2", branch1);

            var branch2 = CreateBranchTree("$/Project/Branch2", trunk);
            var branch3 = CreateBranchTree("$/Project/Branch3", trunk);

            IEnumerable<BranchTree> result = branch1.GetAllChildren();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(new List<BranchTree> { branch1_1, branch1_2 }, result);
        }

        [Fact]
        public void WhenGettingChildrenOfLowerBranch_ThenReturnNothing()
        {
            var trunk = CreateBranchTree("$/Project/Trunk");

            var branch1 = CreateBranchTree("$/Project/Branch1", trunk);

            IEnumerable<BranchTree> result = branch1.GetAllChildren();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void WhenFindingChildrenOfTopBranchByPath_ThenReturnAllTheChildren()
        {
            var trunk = CreateBranchTree("$/Project/Trunk");

            var branch1 = CreateBranchTree("$/Project/Branch1", trunk);
            var branch2 = CreateBranchTree("$/Project/Branch2", trunk);
            var branch3 = CreateBranchTree("$/Project/Branch3", trunk);

            IEnumerable<BranchTree> result = trunk.GetAllChildrenOfBranch("$/Project/Trunk");

            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            Assert.Equal(new List<BranchTree> { branch1, branch2, branch3 }, result);
        }

        [Fact]
        public void WhenFindingChildrenOfOneBranchByPath_ThenReturnChildrenOfThisBranch()
        {
            var trunk = CreateBranchTree("$/Project/Trunk");

            var branch1 = CreateBranchTree("$/Project/Branch1", trunk);
            var branch1_1 = CreateBranchTree("$/Project/Branch1.1", branch1);
            var branch1_2 = CreateBranchTree("$/Project/Branch1.2", branch1);

            var branch2 = CreateBranchTree("$/Project/Branch2", trunk);
            var branch3 = CreateBranchTree("$/Project/Branch3", trunk);

            IEnumerable<BranchTree> result = trunk.GetAllChildrenOfBranch("$/Project/Branch1");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(new List<BranchTree> { branch1_1, branch1_2 }, result);
        }

        [Fact]
        public void WhenFindingChildrenOfLowerBranchByPath_ThenReturnNothing()
        {
            var trunk = CreateBranchTree("$/Project/Trunk");

            var branch1 = CreateBranchTree("$/Project/Branch1", trunk);

            IEnumerable<BranchTree> result = trunk.GetAllChildrenOfBranch("$/Project/Branch1");

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}