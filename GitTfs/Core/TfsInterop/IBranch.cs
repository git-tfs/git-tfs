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
    }
}