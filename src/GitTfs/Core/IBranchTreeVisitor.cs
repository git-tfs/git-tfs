using GitTfs.Core.TfsInterop;

namespace GitTfs.Core
{
    public interface IBranchTreeVisitor
    {
        void Visit(BranchTree childBranch, int level);
    }
}