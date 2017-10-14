using Microsoft.TeamFoundation.VersionControl.Client;
using GitTfs.Core.TfsInterop;

namespace GitTfs.VsCommon
{
    public class WrapperForBranchObject : WrapperFor<BranchObject>, IBranchObject
    {
        BranchObject _branch;

        public WrapperForBranchObject(BranchObject branch) : base(branch)
        {
            _branch = branch;
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
