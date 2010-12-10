using System;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public interface IGitHelpers
    {
        /// <summary>
        /// Runs the given git command, and returns the contents of its STDOUT.
        /// </summary>
        string Command(params string[] command);
        /// <summary>
        /// Runs the given git command, and returns the first line of its STDOUT.
        /// </summary>
        string CommandOneline(params string[] command);
        /// <summary>
        /// Runs the given git command, and passes STDOUT through to the current process's STDOUT.
        /// </summary>
        void CommandNoisy(params string[] command);
        /// <summary>
        /// Runs the given git command, and redirects STDOUT to the provided action.
        /// </summary>
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
