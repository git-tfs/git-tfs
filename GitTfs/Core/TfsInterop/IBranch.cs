using System;
using System.Linq;
using System.Collections.Generic;
using Sep.Git.Tfs.Core.BranchVisitors;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IBranchObject
    {
        string Path { get; }
        string ParentPath { get; }
        bool IsRoot { get; }
    }

    public class BranchTree
    {
        public BranchTree(IBranchObject branch)
            : this(branch, new List<BranchTree>())
        {
        }

        public BranchTree(IBranchObject branch, IEnumerable<BranchTree> childBranches)
            : this(branch, childBranches.ToList())
        {
        }

        public BranchTree(IBranchObject branch, List<BranchTree> childBranches)
        {
            if (childBranches == null)
                throw new ArgumentNullException("childBranches");
            Branch = branch;
            ChildBranches = childBranches;
        }

        public IBranchObject Branch { get; private set; }

        public List<BranchTree> ChildBranches { get; private set; }

        public string Path { get { return Branch.Path; } }

        public override string ToString()
        {
            return string.Format("{0} [{1} children]", this.Path, this.ChildBranches.Count);
        }
    }

    public static class BranchExtensions
    {
        public static BranchTree GetRootTfsBranchForRemotePath(this ITfsHelper tfs, string remoteTfsPath, bool searchExactPath = true)
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
            public static BranchTree Wrap(IBranchObject branch, IEnumerable<IBranchObject> related)
            {
                var children =
                    related.Where(c => c.ParentPath == branch.Path)
                           .Select(c => WrapperForBranchFactory.Wrap(c, related));

                return new BranchTree(branch, children);
            }
        }

        public static void AcceptVisitor(this BranchTree branch, IBranchTreeVisitor treeVisitor, int level = 0)
        {
            treeVisitor.Visit(branch, level);
            foreach (var childBranch in branch.ChildBranches)
            {
                childBranch.AcceptVisitor(treeVisitor, level + 1);
            }
        }

        public static IEnumerable<BranchTree> GetAllChildren(this BranchTree branch)
        {
            if (branch == null) return Enumerable.Empty<BranchTree>();

            var childrenBranches = new List<BranchTree>(branch.ChildBranches);
            foreach (var childBranch in branch.ChildBranches)
            {
                childrenBranches.AddRange(childBranch.GetAllChildren());
            }
            return childrenBranches;
        }
    }
}