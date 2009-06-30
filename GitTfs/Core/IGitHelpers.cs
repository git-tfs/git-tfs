using System;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public interface IGitHelpers
    {
        string TryCommandOneline(params string[] command);
        string CommandOneline(params string[] command);
        void CommandNoisy(params string[] command);
        void Try(string exceptionMessage, Action action);
        [Obsolete("Can this be replaced with a call to structuremap?")]
        IGitRepository MakeRepository(string dir);
    }
}
