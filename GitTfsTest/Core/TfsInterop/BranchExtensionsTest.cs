using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;
using Xunit;

namespace Sep.Git.Tfs.Test.Core.TfsInterop
{
    public class BranchExtensionsTest
    {
        [Fact]
        public void AllChildrenAlwaysReturnsAnEnumerable()
        {
            IEnumerable<BranchTree> result = ((BranchTree) null).GetAllChildren();

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}