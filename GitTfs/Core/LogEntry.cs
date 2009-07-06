using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sep.Git.Tfs.Core
{
    public class LogEntry
    {
        public string AuthorName { get; set; }
        public string Tree { get; set; }

        public IEnumerable<string> CommitParents { get; set; }

        public long ChangesetId { get; set; }

        public string Log { get; set; }

        public string AuthorEmail { get; set; }

        public DateTime Date { get; set; }

        public string CommitterName { get; set; }

        public string CommitterEmail { get; set; }
    }
}
