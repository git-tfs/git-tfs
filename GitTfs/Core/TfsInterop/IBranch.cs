using System;
using System.Linq;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IBranch
    {
        IEnumerable<IBranch> ChildBranches { get; }
        DateTime DateCreated { get; }
        string Path { get; }
    }

    public static class BranchExtensions
    {
        public static void AcceptVisitor(this IBranch branch, IBranchVisitor visitor, int level = 0)
        {
            visitor.Visit(branch, level);
            foreach (var childBranch in branch.ChildBranches)
            {
                childBranch.AcceptVisitor(visitor, level + 1);
            }
        }

        public static IEnumerable<IBranch> GetAllChildren(this IBranch branch)
        {
            if (branch == null) return Enumerable.Empty<IBranch>();

            var childrenBranches = new List<IBranch>(branch.ChildBranches);
            foreach (var childBranch in branch.ChildBranches)
            {
                childrenBranches.AddRange(childBranch.GetAllChildren());
            }
            return childrenBranches;
        }
    }
}