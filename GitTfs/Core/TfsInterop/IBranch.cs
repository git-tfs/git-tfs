using System;
using System.Linq;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.BranchVisitors;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IBranchObject
    {
        DateTime DateCreated { get; }
        string Path { get; }
        string ParentPath { get; }
        bool IsRoot { get; }
    }

    public interface IBranch
    {
        DateTime DateCreated { get; }
        string Path { get; }
        IEnumerable<IBranch> ChildBranches { get; }
    }

    public static class BranchExtensions
    {
        public static IBranch GetRootTfsBranchForRemotePath(this ITfsHelper tfs, string remoteTfsPath, bool searchExactPath = true)
        {
            var branches = tfs.GetBranches();
            var roots = branches.Where(b => b.IsRoot);
            var children = branches.Except(roots);
            var wrapped = roots.Select(b => WrapperForBranchFactory.Wrap(b, children));
            return wrapped.FirstOrDefault(b =>
            {
                var visitor = new BranchTreeContainsPathVisitor(remoteTfsPath, searchExactPath);
                b.AcceptVisitor(visitor);
                return visitor.Found;
            });
        }

        class WrapperForBranchFactory
        {
            public static IBranch Wrap(IBranchObject branch, IEnumerable<IBranchObject> related)
            {
                var children =
                    related.Where(c => c.ParentPath == branch.Path)
                           .Select(c => WrapperForBranchFactory.Wrap(c, related));

                var wrapper = new WrapperForBranch(branch, children);

                return wrapper;
            }
        }

        class WrapperForBranch : IBranch
        {
            IBranchObject _branch;
            IEnumerable<IBranch> _children;

            public WrapperForBranch(IBranchObject branch, IEnumerable<IBranch> children)
            {
                _branch = branch;
                _children = children;
            }

            public DateTime DateCreated { get { return _branch.DateCreated; } }
            public string Path { get { return _branch.Path; } }
            public IEnumerable<IBranch> ChildBranches { get { return _children; } }

            public override string ToString()
            {
                return string.Format("{0} [{1} children]", this.Path, this.ChildBranches.Count());
            }
        }

        public static void AcceptVisitor(this IBranch branch, IBranchTreeVisitor treeVisitor, int level = 0)
        {
            treeVisitor.Visit(branch, level);
            foreach (var childBranch in branch.ChildBranches)
            {
                childBranch.AcceptVisitor(treeVisitor, level + 1);
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