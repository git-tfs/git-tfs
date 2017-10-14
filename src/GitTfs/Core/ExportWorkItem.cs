using System.Diagnostics.Contracts;

namespace GitTfs.Core
{
    public class ExportWorkItem : IExportWorkItem
    {
        public ExportWorkItem(ITfsWorkitem wi = null)
        {
            if (wi != null)
            {
                this.Id = wi.Id.ToString();
                this.Title = wi.Title;
            }
        }
        public ExportWorkItem(string id, string title)
        {
            // id shall be initialized, because it could be used
            // as a key in a dictionary
            Contract.Requires(!string.IsNullOrWhiteSpace(id));
            this.Id = id;

            // Substitute null with the empty string to avoid exceptions
            this.Title = title == null ? string.Empty : title;
        }
        public string Id { get; private set; }

        public string Title { get; private set; }
    }
}
