using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;

namespace Sep.Git.Tfs
{
    public class GitTfs
    {
        private ITfsHelper tfsHelper;
        private GitTfsCommandFactory commandFactory;
        private readonly IHelpHelper _help;
        private readonly IContainer _container;
        private readonly GitTfsCommandRunner _runner;

        public GitTfs(ITfsHelper tfsHelper, GitTfsCommandFactory commandFactory, IHelpHelper help, IContainer container, GitTfsCommandRunner runner)
        {
            this.tfsHelper = tfsHelper;
            this.commandFactory = commandFactory;
            _help = help;
            _container = container;
            _runner = runner;
        }

        public void Run(IList<string> args)
        {
            InitializeGlobals();
            var command = ExtractCommand(args);
            if(RequiresValidGitRepository(command)) AssertValidGitRepository();
            var unparsedArgs = ParseOptions(command, args);
            Main(command, unparsedArgs);
        }

        public void Main(GitTfsCommand command, IList<string> unparsedArgs)
        {
            var globals = _container.GetInstance<Globals>();
            if(globals.ShowHelp)
            {
                Environment.ExitCode = _help.ShowHelp(command);
            }
            else if(globals.ShowVersion)
            {
                _container.GetInstance<TextWriter>().WriteLine(MakeVersionString());
                Environment.ExitCode = GitTfsExitCodes.OK;
            }
            else
            {
                Environment.ExitCode = _runner.Run(command, unparsedArgs);
                //PostFetchCheckout();
            }
        }

        private string MakeVersionString()
        {
            var versionString = "git-tfs version";
            versionString += " " + GetType().Assembly.GetName().Version;
            versionString += GetGitCommitForVersionString();
            versionString += " (TFS client library " + tfsHelper.TfsClientLibraryVersion + ")";
            versionString += " (" + (Environment.Is64BitProcess ? "64-bit" : "32-bit") + ")";
            return versionString;
        }

        private string GetGitCommitForVersionString()
        {
            try
            {
                return " (" + GetGitCommit().Substring(0, 8) + ")";
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to get git version: " + e);
                return "";
            }
        }

        private string GetGitCommit()
        {
            var gitTfsAssembly = GetType().Assembly;
            using (var head = gitTfsAssembly.GetManifestResourceStream("Sep.Git.Tfs.GitVersionInfo"))
            {
                var commitRegex = new Regex(@"commit (?<sha>[a-f0-9]{8})", RegexOptions.IgnoreCase);
                return commitRegex.Match(ReadAllText(head)).Groups["sha"].Value;
            }
        }

        private string ReadAllText(Stream stream)
        {
            return new StreamReader(stream).ReadToEnd().Trim();
        }

        public bool RequiresValidGitRepository(GitTfsCommand command)
        {
            return ! command.GetType().GetCustomAttributes(typeof (RequiresValidGitRepositoryAttribute), false).IsEmpty();
        }

        public void InitializeGlobals()
        {
            var git = _container.GetInstance<IGitHelpers>();
            var globals = _container.GetInstance<Globals>();
            try
            {
                globals.StartingRepositorySubDir = git.CommandOneline("rev-parse", "--show-prefix");
            }
            catch (Exception)
            {
                globals.StartingRepositorySubDir = "";
            }
            if(globals.GitDir != null)
            {
                globals.GitDirSetByUser = true;
            }
            else
            {
                globals.GitDir = ".git";
            }
            globals.RemoteId = GitTfsConstants.DefaultRepositoryId;
        }

        public void AssertValidGitRepository()
        {
            var globals = _container.GetInstance<Globals>();
            var git = _container.GetInstance<IGitHelpers>();
            if (!Directory.Exists(globals.GitDir))
            {
                if (globals.GitDirSetByUser)
                {
                    throw new Exception("GIT_DIR=" + globals.GitDir + " explicitly set, but it is not a directory.");
                }
                var gitDir = globals.GitDir;
                globals.GitDir = null;
                string cdUp = null;
                git.WrapGitCommandErrors("Already at toplevel, but " + gitDir + " not found.",
                                         () =>
                                             {
                                                 cdUp = git.CommandOneline("rev-parse", "--show-cdup");
                                                 if (String.IsNullOrEmpty(cdUp))
                                                     gitDir = ".";
                                                 else
                                                     cdUp = cdUp.TrimEnd();
                                                 if (String.IsNullOrEmpty(cdUp))
                                                     cdUp = ".";
                                             });
                Environment.CurrentDirectory = cdUp;
                if (!Directory.Exists(gitDir))
                {
                    throw new Exception(gitDir + " still not found after going to " + cdUp);
                }
                globals.GitDir = gitDir;
            }
            globals.Repository = git.MakeRepository(globals.GitDir);
        }

        public GitTfsCommand ExtractCommand(IList<string> args)
        {
            for (int i = 0; i < args.Count; i++)
            {
                var command = commandFactory.GetCommand(args[i]);
                if (command != null)
                {
                    args.RemoveAt(i);
                    return command;
                }
            }
            return _container.GetInstance<Help>();
        }

        public IList<string> ParseOptions(GitTfsCommand command, IList<string> args)
        {
            foreach (var parseHelper in command.GetOptionParseHelpers(_container))
            {
                var parser = new Parser(parseHelper);
                args = parser.Parse(args.ToArray());
            }
            return args;
        }
    }
}
