
namespace Sep.Git.Tfs.Core.TfsInterop
{
    public interface IPendingChange
    {
        string FileName { get; }
        bool IsLock { get; }
        TfsLockLevel LockLevel { get; }
        string LocalItem { get; }
        string ServerItem { get; }
    }

    public enum TfsLockLevel 
    {
        None = 0,
        Checkin = 1,
        CheckOut = 2,
        Unchanged = 3
    }
}