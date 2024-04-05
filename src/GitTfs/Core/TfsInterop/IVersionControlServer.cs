namespace GitTfs.Core.TfsInterop
{
    public interface IVersionControlServer
    {
        IItem GetItem(int itemId, int changesetNumber);
        IItem GetItem(string itemPath, int changesetNumber);
        IItem[] GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType);
        IEnumerable<IChangeset> QueryHistory(string path, int version, int deletionId, TfsRecursionType recursion,
            string user, int versionFrom, int versionTo, int maxCount, bool includeChanges, bool slotMode,
            bool includeDownloadInfo);
    }
}
