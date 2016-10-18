
namespace Sep.Git.Tfs.Core
{
    public interface ITfsWorkitem
    {
        int Id { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        string Url { get; set; }
    }
}