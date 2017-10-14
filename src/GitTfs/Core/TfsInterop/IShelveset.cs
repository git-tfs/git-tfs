
namespace GitTfs.Core.TfsInterop
{
    public interface IShelveset
    {
        string Comment { get; set; }
        IWorkItemCheckinInfo[] WorkItemInfo { get; set; }
    }
}