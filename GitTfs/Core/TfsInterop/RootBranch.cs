using System.Diagnostics;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class RootBranch
    {
        public string TfsBranchPath { get; private set; }

        public int SourceBranchChangesetId { get; private set; }

        public int TargetBranchChangesetId { get; private set; }

        public RootBranch(int sourceBranchChangesetId, string tfsBranchPath)
            : this(sourceBranchChangesetId, -1, tfsBranchPath)
        {

        }

        public RootBranch(int sourceBranchChangesetId, int targetBranchChangesetId, string tfsBranchPath)
        {
            SourceBranchChangesetId = sourceBranchChangesetId;
            TargetBranchChangesetId = targetBranchChangesetId;
            TfsBranchPath = tfsBranchPath;
        }

        public bool IsRenamedBranch { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                if (TargetBranchChangesetId > -1)
                    return string.Format("{0} C{1} (target C{2}){3}", TfsBranchPath, SourceBranchChangesetId, TargetBranchChangesetId, IsRenamedBranch ? " renamed" : "");

                return string.Format("{0} C{1}{2}", TfsBranchPath, SourceBranchChangesetId, IsRenamedBranch ? " renamed" : "");
            }
        }
    }
}