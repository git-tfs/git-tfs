namespace Sep.Git.Tfs.Core.TfsInterop
{
    public class RootBranch
    {
        public RootBranch(int rootChangeset, string tfsBranchPath)
        {
            RootChangeset = rootChangeset;
            TfsBranchPath = tfsBranchPath;
        }

        public int RootChangeset { get; private set; }
        public string TfsBranchPath { get; private set; }
        public bool IsRenamedBranch { get; set; }
    }
}