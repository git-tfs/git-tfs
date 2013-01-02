using System.Linq;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core.BranchVisitors
{
    public class BranchContainsPathVisitor : IBranchVisitor
    {
        private string searchPath;
        private bool searchExactPath;

        public BranchContainsPathVisitor(string searchPath, bool searchExactPath)
        {
            this.searchPath = searchPath;
            this.searchExactPath = searchExactPath;
        }

        public bool Found { get; private set; }

        public void Visit(IBranch childBranch, int level)
        {
            if (Found == false
                && ((searchExactPath && searchPath.ToLower() == childBranch.Path.ToLower())
                || (!searchExactPath && searchPath.ToLower().IndexOf(childBranch.Path.ToLower()) == 0)))
            {
                Found = true;
            }
        }
    }
}