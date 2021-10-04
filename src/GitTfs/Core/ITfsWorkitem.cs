
namespace GitTfs.Core
{
    public interface ITfsWorkitem
    {
        int Id { get; set; }
        string Title { get; set; }
        string Url { get; set; }
    }
}