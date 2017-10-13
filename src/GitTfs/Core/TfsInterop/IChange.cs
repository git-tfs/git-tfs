
namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IChange
    {
        TfsChangeType ChangeType { get; }
        IItem Item { get; }
    }
}