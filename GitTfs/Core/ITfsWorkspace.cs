namespace Sep.Git.Tfs.Core
{
    public interface ITfsWorkspace
    {
        /// <summary>
        /// Shelves all pending changes, with the given shelveset name.
        /// </summary>
        void Shelve(string shelvesetName);

        string GetLocalPath(string path);
        void Add(string path);
        void Edit(string path);
        void Delete(string path);
        void Rename(string pathFrom, string pathTo, string score);
    }
}