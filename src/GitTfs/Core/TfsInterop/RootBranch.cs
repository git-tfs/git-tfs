using System.Diagnostics;

namespace GitTfs.Core.TfsInterop
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class RootBranch
    {
        public RootBranch(int sourceBranchChangesetId, string tfsSourceBranchPath, string tfsBranchPath)
            : this(sourceBranchChangesetId, tfsSourceBranchPath, -1, tfsBranchPath)
        {
        }

        public RootBranch(int sourceBranchChangesetId, string tfsSourceBranchPath, int targetBranchChangesetId, string tfsBranchPath)
        {
            SourceBranchChangesetId = sourceBranchChangesetId;
            TargetBranchChangesetId = targetBranchChangesetId;
            TfsSourceBranchPath = tfsSourceBranchPath;
            TfsBranchPath = tfsBranchPath;
        }

        public int SourceBranchChangesetId { get; }
        public int TargetBranchChangesetId { get; }
        public string TfsSourceBranchPath { get; }
        public string TfsBranchPath { get; }
        public bool IsRenamedBranch { get; set; }

        private string DebuggerDisplay => string.Format("{0} C{1}{2}{3}{4}",
                    /* {0} */ TfsBranchPath,
                    /* {1} */ SourceBranchChangesetId,
                    /* {2} */ TargetBranchChangesetId > -1 ? $" (target C{TargetBranchChangesetId})" : string.Empty,
                    /* {3} */ IsRenamedBranch ? " renamed" : "",
                    /* {4} */ "Source " + TfsSourceBranchPath
                );
    }
}