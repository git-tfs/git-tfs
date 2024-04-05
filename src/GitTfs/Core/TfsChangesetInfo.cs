namespace GitTfs.Core
{
    public class TfsChangesetInfo
    {
        public IGitTfsRemote Remote { get; set; }
        public int ChangesetId { get; set; }
        public string GitCommit { get; set; }
        public IEnumerable<ITfsWorkitem> Workitems { get; set; }

        public IEnumerable<ITfsCheckinNote> CheckinNotes { get; set; }

        public string PolicyOverrideComment { get; set; }

        public TfsChangesetInfo()
        {
            Workitems = Enumerable.Empty<ITfsWorkitem>();
            CheckinNotes = Enumerable.Empty<ITfsCheckinNote>();
        }
    }
}
