using System;

namespace Sep.Git.Tfs.Core
{
    public interface IGitHelpers
    {
        string TryCommandOneline(string gitCommand, params string [] args);
        string CommandOneline(string gitCommand, params string [] args);
        void Try(string exceptionMessage, Action action);
        IGit MakeRepository(string dir);
    }
}
