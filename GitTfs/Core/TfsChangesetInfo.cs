using System.Collections.Generic;
namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo
    {
        public IGitTfsRemote Remote { get; set; }
        public long ChangesetId { get; set; }
        public string GitCommit { get; set; }
        public IEnumerable<ITfsWorkitem> Workitems { get; set; }
    }
}
