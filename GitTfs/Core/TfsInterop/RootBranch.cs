using System.Diagnostics;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    [DebuggerDisplay("{DebuggerDisplay}")]
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

        private string DebuggerDisplay
        {
            get { return string.Format("{0} C{1}{2}", TfsBranchPath, RootChangeset, (IsRenamedBranch ? " renamed" : "")); }
        }
    }
}