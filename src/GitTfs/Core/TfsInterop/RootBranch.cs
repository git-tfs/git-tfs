using System.Diagnostics;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class RootBranch
    {
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

        public int SourceBranchChangesetId { get; private set; }
        public int TargetBranchChangesetId { get; private set; }
        public string TfsBranchPath { get; private set; }
        public bool IsRenamedBranch { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format("{0} C{1}{2}{3}",
                    /* {0} */ TfsBranchPath,
                    /* {1} */ SourceBranchChangesetId,
                    /* {2} */ TargetBranchChangesetId > -1 ? string.Format(" (target C{0})", TargetBranchChangesetId) : string.Empty,
                    /* {3} */ IsRenamedBranch ? " renamed" : ""
                );
            }
        }
    }
}