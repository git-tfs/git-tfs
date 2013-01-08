using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.VsCommon
{
    public class WrapperForBranchObject : WrapperFor<BranchObject>, IBranchObject
    {
        BranchObject _branch;

        public WrapperForBranchObject(BranchObject branch) : base(branch)
        {
            _branch = branch;
        }

        public DateTime DateCreated
        {
            get { return _branch.DateCreated; }
        }

        public string Path
        {
            get { return _branch.Properties.RootItem.Item; }
        }

        public bool IsRoot
        {
            get { return _branch.Properties.ParentBranch == null; }
        }

        public string ParentPath
        {
            get { return _branch.Properties.ParentBranch.Item; }
        }
    }
}
