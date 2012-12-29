using System.Linq;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public interface IBranchVisitor
    {
        void Visit(IBranch childBranch, int level);
    }
}