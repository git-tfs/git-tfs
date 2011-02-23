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
        void CommandInputPipe(Action<TextWriter> action, params string[] command);
        void CommandInputOutputPipe(Action<TextWriter, TextReader> interact, params string[] command);
        void WrapGitCommandErrors(string exceptionMessage, Action action);
        IGitRepository MakeRepository(string dir);
    }

    public static partial class Ext
    {
        public static void SetConfig(this IGitHelpers gitHelpers, string configKey, object value)
        {
            gitHelpers.CommandNoisy("config", configKey, value.ToString());
        }
    }
}
