
namespace Sep.Git.Tfs.Core
{
    public class TfsWorkitem : ITfsWorkitem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
}