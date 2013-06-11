using System;
using System.Collections.Generic;
using System.Linq;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo : IDisposable
    {
        public IGitTfsRemote Remote { get; set; }
        public long ChangesetId { get; set; }
        public string GitCommit { get; set; }
        public IEnumerable<ITfsWorkitem> Workitems { get; set; }

        public TfsChangesetInfo()
        {
            Workitems = Enumerable.Empty<ITfsWorkitem>();
        }

        public void Dispose()
        {
            Workitems = null;
        }
    }
}
