﻿using System.Collections.Generic;
using System.Linq;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo
    {
        public IGitTfsRemote Remote { get; set; }
        public long ChangesetId { get; set; }
        public string GitCommit { get; set; }
        public IEnumerable<ITfsWorkitem> Workitems { get; set; }

        public string CodeReviewer { get; set; }
        public string SecurityReviewer { get; set; }
        public string PerformanceReviewer { get; set; }

        public string PolicyOverrideComment { get; set; }

        public TfsChangesetInfo()
        {
            Workitems = Enumerable.Empty<ITfsWorkitem>();
        }
    }
}
