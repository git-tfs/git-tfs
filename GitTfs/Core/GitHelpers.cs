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
        private readonly TextWriter realStdout;
        private readonly IContainer _container;
        public const string DEFAULT_GIT_DIR = ".git";

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

        public class ProcessStdoutReader : TextReader
        {
            private readonly Process process;
            private readonly GitHelpers helper;

            public ProcessStdoutReader(GitHelpers helper, Process process)
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
                                  action(process.StandardInput.WithDefaultEncoding());
                                  Close(process);
                              });
        }

        public void CommandInputOutputPipe(Action<TextWriter, TextReader> interact, params string[] command)
        {
            Time(command, () =>
                              {
                                  AssertValidCommand(command);
                                  var process = Start(command, Ext.And<ProcessStartInfo>(RedirectStdin, RedirectStdout));
                                  var encoding = new UTF8Encoding(false);
                                  interact(new StreamWriter(process.StandardInput.BaseStream, encoding), new StreamReader(process.StandardOutput.BaseStream, encoding));
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

        private void Close(Process process)
        {
            // if caller doesn't read entire stdout to the EOF - it is possible that 
            // child process will hang waiting until there will be free space in stdout
            // buffer to write the rest of the output. To prevent such situation we'll
            // close stdout to indicate we're no more interested in it, thus allowing
            // child process to proceed.
            // See https://github.com/git-tfs/git-tfs/issues/121 for details.
            if (process.StartInfo.RedirectStandardOutput)
                process.StandardOutput.Close();

            if (!process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds))
                throw new GitCommandException("Command did not terminate.", process);
            if(process.ExitCode != 0)
                throw new GitCommandException(string.Format("Command exited with error code: {0}", process.ExitCode), process);
        }

        private void RedirectStdout(ProcessStartInfo startInfo)
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.StandardOutputEncoding = Encoding.Default;
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
            process.ErrorDataReceived += StdErrReceived;
            process.BeginErrorReadLine();
            return process;
        }

        private void StdErrReceived(object sender, DataReceivedEventArgs e)
        {
            if(e.Data != null && e.Data.Trim() != "")
            {
                Trace.WriteLine(e.Data.TrimEnd(), "git stderr");
            }
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
                throw new Exception("bad command: " + (command.Length == 0 ? "" : command[0]));
        }

        public static DirectoryInfo ResolveRepositoryLocation()
        {
            // special case for a submodule.
            if (File.Exists(DEFAULT_GIT_DIR))
            {
                // Parse out the location of the submodule.
                using (StreamReader StreamReader = new StreamReader(DEFAULT_GIT_DIR))
                {
                    string GitDirConfig = "gitdir:";
                    string Line;
                    while ((Line = StreamReader.ReadLine()) != null)
                    {
                        // Skip the unneeded lines.
                        if (!Line.Trim().ToLower().StartsWith(GitDirConfig))
                        {
                            continue;
                        }

                        // Get out the path of the submodule via the relative path stored in the file.
                        string SubmoduleGitDirectoryRelativePath = Line.Replace(GitDirConfig, "").Trim();

                        // Return submodule repo.
                        return new DirectoryInfo(SubmoduleGitDirectoryRelativePath);
                    }
                }
            }

            return new DirectoryInfo(DEFAULT_GIT_DIR);
        }
    }
}
