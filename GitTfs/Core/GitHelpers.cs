using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class GitHelpers : IGitHelpers
    {
        private readonly TextWriter realStdout;

        public GitHelpers(TextWriter stdout)
        {
            realStdout = stdout;
        }

        public string Command(params string[] command)
        {
            string retVal = null;
            CommandOutputPipe(stdout => retVal = stdout.ReadToEnd(), command);
            return retVal;
        }

        public string CommandOneline(params string[] command)
        {
            string retVal = null;
            CommandOutputPipe(stdout => retVal = stdout.ReadLine(), command);
            return retVal;
        }

        public void CommandNoisy(params string[] command)
        {
            CommandOutputPipe(stdout => realStdout.Write(stdout.ReadToEnd()), command);
        }

        public void CommandOutputPipe(Action<TextReader> handleOutput, params string[] command)
        {
            AssertValidCommand(command);
            var process = Start(command, RedirectStdout);
            handleOutput(process.StandardOutput);
            Close(process);
        }

        public void CommandInputPipe(Action<TextWriter> action, params string[] command)
        {
            AssertValidCommand(command);
            var process = Start(command, RedirectStdin);
            action(process.StandardInput);
            Close(process);
        }

        public void CommandInputOutputPipe(Action<TextWriter, TextReader> interact, params string[] command)
        {
            AssertValidCommand(command);
            var process = Start(command, Ext.And<ProcessStartInfo>(RedirectStdin, RedirectStdout));
            interact(process.StandardInput, process.StandardOutput);
            Close(process);
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

        private void RedirectStdin(ProcessStartInfo startInfo)
        {
            startInfo.RedirectStandardInput = true;
        }

        private Process Start(string[] command)
        {
            return Start(command, x => {});
        }

        protected virtual Process Start(string [] command, Action<ProcessStartInfo> initialize)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "git";
            startInfo.SetArguments(command);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            initialize(startInfo);
            Trace.WriteLine("Starting process: " + startInfo.FileName + " " + startInfo.Arguments, "git command");
            var process = Process.Start(startInfo);
            process.ErrorDataReceived += (sender, e) => Trace.WriteLine(e.Data, "git stderr");
            process.BeginErrorReadLine();
            return process;
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
            return ObjectFactory.With("gitDir").EqualTo(dir).GetInstance<IGitRepository>();
        }

        private static readonly Regex ValidCommandName = new Regex("^[a-z0-9A-Z_-]+$");
        private static void AssertValidCommand(string[] command)
        {
            if(command.Length < 1 || !ValidCommandName.IsMatch(command[0]))
                throw new Exception("bad command: " + (command.Length == 0 ? "" : command[0]));
        }
    }
}
