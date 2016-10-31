using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core.BranchVisitors
{
    public class BranchTreeContainsPathVisitor : IBranchTreeVisitor
    {
        private readonly string _searchPath;
        private readonly bool _searchExactPath;

        public BranchTreeContainsPathVisitor(string searchPath, bool searchExactPath)
        {
            _searchPath = searchPath;
            _searchExactPath = searchExactPath;
        }

        public bool Found { get; private set; }

        public void Visit(BranchTree childBranch, int level)
        {
            if (Found == false
                && ((_searchExactPath && _searchPath.ToLower() == childBranch.Path.ToLower())
                || (!_searchExactPath && _searchPath.ToLower().IndexOf(childBranch.Path.ToLower()) == 0)))
            {
                Found = true;
            }
        }
    }
}