using System;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public interface IGitHelpers
    {
        string Command(params string[] command);
        string CommandOneline(params string[] command);
        void CommandNoisy(params string[] command);
        void CommandOutputPipe(Action<TextReader> func, params string[] command);
        void CommandInputOutputPipe(Action<TextWriter, TextReader> interact, params string[] command);
        void WrapGitCommandErrors(string exceptionMessage, Action action);
        [Obsolete("Can this be replaced with a call to structuremap?")]
        IGitRepository MakeRepository(string dir);
    }
}
