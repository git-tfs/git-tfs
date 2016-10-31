using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public interface IBranchTreeVisitor
    {
        void Visit(BranchTree childBranch, int level);
    }
}