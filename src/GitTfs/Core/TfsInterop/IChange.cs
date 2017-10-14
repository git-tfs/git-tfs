
namespace GitTfs.Core.TfsInterop
{
    public interface IChange
    {
        TfsChangeType ChangeType { get; }
        IItem Item { get; }
    }
}