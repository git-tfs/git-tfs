using System;
namespace Sep.Git.Tfs.Core
{
    public interface ITfsWorkspace : IDisposable
    {
        /// <summary>
        /// Shelves all pending changes, with the given shelveset name.
        /// </summary>
        void Shelve(string shelvesetName);
    }
}