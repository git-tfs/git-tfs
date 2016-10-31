using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core.BranchVisitors
{
    public class BranchTreeContainsPathVisitor : IBranchTreeVisitor
    {
        private readonly string searchPath;
        private readonly bool searchExactPath;

        public BranchTreeContainsPathVisitor(string searchPath, bool searchExactPath)
        {
            this.searchPath = searchPath;
            this.searchExactPath = searchExactPath;
        }

        public bool Found { get; private set; }

        public void Visit(BranchTree childBranch, int level)
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