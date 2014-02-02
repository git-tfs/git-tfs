﻿using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class LogEntry
    {
        public LogEntry()
        {
            CommitParents = new List<string>();
        }

        public string AuthorName { get; set; }
        public Tree Tree { get; set; }

        public IList<string> CommitParents { get; private set; }

        public long ChangesetId { get; set; }

        public string Log { get; set; }

        public string AuthorEmail { get; set; }

        public DateTime Date { get; set; }

        public string CommitterName { get; set; }

        public string CommitterEmail { get; set; }

        public IGitTfsRemote Remote { get; set; }
    }
}
