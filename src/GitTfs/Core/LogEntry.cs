using LibGit2Sharp;

namespace GitTfs.Core
{
    public class LogEntry
    {
        public LogEntry()
        {
            CommitParents = new List<string>();
        }

        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public string CommitterName { get; set; }
        public string CommitterEmail { get; set; }

        public DateTime Date { get; set; }
        public string Log { get; set; }

        public Tree Tree { get; set; }
        public IList<string> CommitParents { get; private set; }

        public int ChangesetId { get; set; }
        public IGitTfsRemote Remote { get; set; }
    }
}
