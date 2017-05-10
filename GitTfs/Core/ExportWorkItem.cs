namespace Sep.Git.Tfs.Core
{
    public class ExportWorkItem : IExportWorkItem
    {
        public ExportWorkItem(ITfsWorkitem wi = null)
        {
            if(wi!=null)
            {
                this.Id = wi.Id.ToString();
                this.Title = wi.Title;
            }
        }
        public string Id { get; set; }

        public string Title { get; set; }
    }
}
