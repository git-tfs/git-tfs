using System;

namespace Sep.Git.Tfs.Core
{
    public interface ITfsWorkspace
    {
        /// <summary>
        /// Shelves all pending changes, with the given shelveset name.
        /// </summary>
        void Shelve(string shelvesetName, bool evaluateCheckinPolicies, Func<string> generateCheckinComment);
        /// <summary>
        /// Evaluates check-in policies and checks in all pending changes.
        /// </summary>
        long Checkin();

        string GetLocalPath(string path);
        void Add(string path);
        void Edit(string path);
        void Delete(string path);
        void Rename(string pathFrom, string pathTo, string score);
        long CheckinTool(Func<string> generateCheckinComment);
    }
}