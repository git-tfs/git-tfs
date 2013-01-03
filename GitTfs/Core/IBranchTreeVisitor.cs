using System.Linq;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public interface IBranchTreeVisitor
    {
        void Visit(IBranch childBranch, int level);
    }
}