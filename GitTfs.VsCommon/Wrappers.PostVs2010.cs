using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public class WrapperForBranch : IBranch
    {
        private readonly BranchObject branch;

        public WrapperForBranch(BranchObject branch, IEnumerable<IBranch> children)
        {
            this.branch = branch;
            this.ChildBranches = children;
        }

        public BranchObject WrappedBranch { get { return this.branch; } }

        public IEnumerable<IBranch> ChildBranches { get; private set; }

        public DateTime DateCreated { get { return branch.DateCreated; } }

        public string Path { get { return branch.Properties.RootItem.Item; } }

        public override string ToString()
        {
            return string.Format("{0} [{1} children]", this.Path, this.ChildBranches.Count());
        }
    }

    public class WrapperForBranchFactory
    {
        public static WrapperForBranch Wrap(BranchObject branch, IList<BranchObject> related)
        {
            var children =
                related.Where(c => c.Properties.ParentBranch.Item == branch.Properties.RootItem.Item)
                       .Select(c => WrapperForBranchFactory.Wrap(c, related));

            var wrapper = new WrapperForBranch(branch, children);

            return wrapper;
        }
    }
}
