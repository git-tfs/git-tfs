using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using StructureMap;
using System.Text;

namespace Sep.Git.Tfs.Core
{
    public class GitHelpers : IGitHelpers
    {
        protected readonly TextWriter realStdout;
        private readonly IContainer _container;

        /// <summary>
        /// Starting with version 1.7.10, Git uses UTF-8.
        /// Use this encoding for Git input and output.
        /// </summary>
        private static Encoding _encoding = new UTF8Encoding(false, true);

        public GitHelpers(TextWriter stdout, IContainer container)
        {
            realStdout = stdout;
            _container = container;
        }

        /// <summary>
        /// Runs the given git command, and returns the contents of its STDOUT.
        /// </summary>
        public string Command(params string[] command)
        {
            string retVal = null;
            CommandOutputPipe(stdout => retVal = stdout.ReadToEnd(), command);
            return retVal;
        }

        /// <summary>
        /// Runs the given git command, and returns the first line of its STDOUT.
        /// </summary>
        public string CommandOneline(params string[] command)
        {
            string retVal = null;
            CommandOutputPipe(stdout => retVal = stdout.ReadLine(), command);
            return retVal;
        }

        /// <summary>
        /// Runs the given git command, and passes STDOUT through to the current process's STDOUT.
        /// </summary>
        public void CommandNoisy(params string[] command)
        {
            CommandOutputPipe(stdout => realStdout.Write(stdout.ReadToEnd()), command);
        }

        /// <summary>
        /// Runs the given git command, and redirects STDOUT to the provided action.
        /// </summary>
        public void CommandOutputPipe(Action<TextReader> handleOutput, params string[] command)
        {
            Time(command, () =>
                              {
                                  AssertValidCommand(command);
                                  var process = Start(command, RedirectStdout);
                                  handleOutput(process.StandardOutput);
                                  Close(process);
                              });
        }

        /// <summary>
        /// Runs the given git command, and returns a reader for STDOUT. NOTE: The returned value MUST be disposed!
        /// </summary>
        public TextReader CommandOutputPipe(params string[] command)
        {
            AssertValidCommand(command);
            var process = Start(command, RedirectStdout);
            return new ProcessStdoutReader(this, process);
        }

        class ProcessStdoutReader : TextReader
        {
            private readonly GitProcess process;
            private readonly GitHelpers helper;

            public ProcessStdoutReader(GitHelpers helper, GitProcess process)
            {
                this.helper = helper;
                this.process = process;
            }

            public override void Close()
            {
                helper.Close(process);
            }

            public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
            {
                return process.StandardOutput.CreateObjRef(requestedType);
            }

            protected override void Dispose(bool disposing)
            {
                if(disposing && process != null)
                {
                    Close();
                }
                base.Dispose(disposing);
            }

            public override bool Equals(object obj)
            {
                return process.StandardOutput.Equals(obj);
            }

            public override int GetHashCode()
            {
                return process.StandardOutput.GetHashCode();
            }

            public override object InitializeLifetimeService()
            {
                return process.StandardOutput.InitializeLifetimeService();
            }

            public override int Peek()
            {
                return process.StandardOutput.Peek();
            }

            public override int Read()
            {
                return process.StandardOutput.Read();
            }

            public override int Read(char[] buffer, int index, int count)
            {
                return process.StandardOutput.Read(buffer, index, count);
            }

            public override int ReadBlock(char[] buffer, int index, int count)
            {
                return process.StandardOutput.ReadBlock(buffer, index, count);
            }

            public override string ReadLine()
            {
                return process.StandardOutput.ReadLine();
            }

            public override string ReadToEnd()
            {
                return process.StandardOutput.ReadToEnd();
            }

            public override string ToString()
            {
                return process.StandardOutput.ToString();
            }
        }

        public void CommandInputPipe(Action<TextWriter> action, params string[] command)
        {
            Time(command, () =>
                              {
                                  AssertValidCommand(command);
                                  var process = Start(command, RedirectStdin);
                                  action(process.StandardInput.WithEncoding(_encoding));
                                  Close(process);
                              });
        }

        public void CommandInputOutputPipe(Action<TextWriter, TextReader> interact, params string[] command)
        {
            Time(command, () =>
                              {
                                  AssertValidCommand(command);
                                  var process = Start(command, Ext.And<ProcessStartInfo>(RedirectStdin, RedirectStdout));
                                  interact(process.StandardInput.WithEncoding(_encoding), process.StandardOutput);
                                  Close(process);
                              });
        }

        private void Time(string[] command, Action action)
        {
            var start = DateTime.Now;
            try
            {
                action();
            }
            finally
            {
                var end = DateTime.Now;
                Trace.WriteLine(String.Format("[{0}] {1}", end - start, String.Join(" ", command)), "git command time");
            }
        }

        private void Close(GitProcess process)
        {
            // if caller doesn't read entire stdout to the EOF - it is possible that 
            // child process will hang waiting until there will be free space in stdout
            // buffer to write the rest of the output.
            // See https://github.com/git-tfs/git-tfs/issues/121 for details.
            if (process.StartInfo.RedirectStandardOutput)
            {
                process.StandardOutput.BaseStream.CopyTo(Stream.Null);
                process.StandardOutput.Close();
            }

            if (!process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds))
                throw new GitCommandException("Command did not terminate.", process);
            if(process.ExitCode != 0)
                throw new GitCommandException(string.Format("Command exited with error code: {0}\n{1}", process.ExitCode, process.StandardErrorString), process);
        }

        private void RedirectStdout(ProcessStartInfo startInfo)
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.StandardOutputEncoding = _encoding;
        }

        private void RedirectStderr(ProcessStartInfo startInfo)
        {
           startInfo.RedirectStandardError= true;
           startInfo.StandardErrorEncoding = _encoding;
        }

        private void RedirectStdin(ProcessStartInfo startInfo)
        {
            startInfo.RedirectStandardInput = true;
            // there is no StandardInputEncoding property, use extension method StreamWriter.WithEncoding instead
        }

        private GitProcess Start(string[] command)
        {
            return Start(command, x => {});
        }

        protected virtual GitProcess Start(string [] command, Action<ProcessStartInfo> initialize)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "git";
            startInfo.SetArguments(command);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.EnvironmentVariables.Add("GIT_PAGER", "cat");
            RedirectStderr(startInfo);
            initialize(startInfo);
            Trace.WriteLine("Starting process: " + startInfo.FileName + " " + startInfo.Arguments, "git command");
            var process = new GitProcess(Process.Start(startInfo));
            process.ConsumeStandardError();
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
            return _container
                .With("gitDir").EqualTo(dir)
                .GetInstance<IGitRepository>();
        }

        private static readonly Regex ValidCommandName = new Regex("^[a-z0-9A-Z_-]+$");
        private static void AssertValidCommand(string[] command)
        {
            if(command.Length < 1 || !ValidCommandName.IsMatch(command[0]))
                throw new Exception("bad git command: " + (command.Length == 0 ? "" : command[0]));
        }

        protected class GitProcess
        {
            Process _process;

            public GitProcess(Process process)
            {
                _process = process;
            }

            public static implicit operator Process(GitProcess process)
            {
                return process._process;
            }

            public string StandardErrorString { get; private set; }

            public void ConsumeStandardError()
            {
                StandardErrorString = "";
                _process.ErrorDataReceived += StdErrReceived;
                _process.BeginErrorReadLine();
            }

            private void StdErrReceived(object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null && e.Data.Trim() != "")
                {
                    var data = e.Data;
                    Trace.WriteLine(data.TrimEnd(), "git stderr");
                    StandardErrorString += data;
                }
            }

            // Delegate a bunch of things to the Process.

            public ProcessStartInfo StartInfo { get { return _process.StartInfo; } }
            public int ExitCode { get { return _process.ExitCode; } }

            public StreamWriter StandardInput { get { return _process.StandardInput; } }
            public StreamReader StandardOutput { get { return _process.StandardOutput; } }

            public bool WaitForExit(int milliseconds)
            {
                return _process.WaitForExit(milliseconds);
            }
        }
    }
}
