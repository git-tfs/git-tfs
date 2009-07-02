using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Sep.Git.Tfs.Core
{
    public class GitHelpers : IGitHelpers
    {
        public string CommandOneline(params string[] command)
        {
            AssertValidCommand(command);
            var process = Start(command, RedirectStdout);
            var returnValue = process.StandardOutput.ReadLine();
            Close(process);
            return returnValue;
        }

        private void Close(Process process)
        {
            if (!process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds))
                throw new GitCommandException("Command did not terminate.", process);
            if(process.ExitCode != 0)
                throw new GitCommandException("Command exited with error code.", process);
        }

        private void RedirectStdout(ProcessStartInfo startInfo)
        {
            startInfo.RedirectStandardOutput = true;
        }

        private Process Start(string[] command)
        {
            return Start(command, null);
        }

        protected virtual Process Start(string [] command, Action<ProcessStartInfo> initialize)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "git";
            startInfo.SetArguments(command);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            if(initialize != null) initialize(startInfo);
            Trace.WriteLine("Starting process: " + startInfo.FileName + " " + startInfo.Arguments);
            return Process.Start(startInfo);
        }

        public void CommandNoisy(params string[] command)
        {
            AssertValidCommand(command);
            Close(Start(command));
        }

        /// <summary>
        /// WrapGitCommandErrors the actions, and if there are any git exceptions, rethrow a new exception with the given message.
        /// </summary>
        /// <param name="exceptionMessage">A friendlier message to wrap the GitCommandException with. {0} is replaced with the command line and {1} is replaced with the exit code.</param>
        /// <param name="action"></param>
        public void WrapGitCommandErrors(string exceptionMessage, Action action)
        {
            try
            {
                action();
            }
            catch (GitCommandException e)
            {
                throw new Exception(String.Format(exceptionMessage, e.Process.StartInfo.FileName + " " + e.Process.StartInfo.Arguments, e.Process.ExitCode), e);
            }
        }

        public IGitRepository MakeRepository(string dir)
        {
            return null;
        }

        private static readonly Regex ValidCommandName = new Regex("^[a-z0-9A-Z_-]+$");
        private static void AssertValidCommand(string[] command)
        {
            if(command.Length < 1 || !ValidCommandName.IsMatch(command[0]))
                throw new Exception("bad command: " + (command.Length == 0 ? "" : command[0]));
        }
    }
}
