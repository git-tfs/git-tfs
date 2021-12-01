using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StructureMap;
using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Util;
using NLog;

namespace GitTfs
{
    public class GitTfs
    {
        private readonly IGitTfsVersionProvider _gitTfsVersionProvider;
        private readonly GitTfsCommandFactory _commandFactory;
        private readonly IHelpHelper _help;
        private readonly IContainer _container;
        private readonly GitTfsCommandRunner _runner;
        private readonly Globals _globals;
        private readonly Bootstrapper _bootstrapper;
        private readonly AuthorsFile _authorsFileHelper;

        public GitTfs(GitTfsCommandFactory commandFactory, IHelpHelper help, IContainer container,
            IGitTfsVersionProvider gitTfsVersionProvider, GitTfsCommandRunner runner, Globals globals, Bootstrapper bootstrapper, AuthorsFile authorsFileHelper)
        {
            _commandFactory = commandFactory;
            _help = help;
            _container = container;
            _gitTfsVersionProvider = gitTfsVersionProvider;
            _runner = runner;
            _globals = globals;
            _bootstrapper = bootstrapper;
            _authorsFileHelper = authorsFileHelper;
        }

        public int Run(IList<string> args)
        {
            InitializeGlobals();
            _globals.CommandLineRun = "git tfs " + string.Join(" ", args);
            var command = ExtractCommand(args);
            var unparsedArgs = ParseOptions(command, args);
            UpdateLoggerOnDebugging();
            Trace.WriteLine("Command run:" + _globals.CommandLineRun);
            if (RequiresValidGitRepository(command)) AssertValidGitRepository();
            bool willCreateRepository = command.GetType() == typeof(Clone) || command.GetType() == typeof(Init);
            ParseAuthorsAndSave(!willCreateRepository);
            var exitCode = Main(command, unparsedArgs);
            if (willCreateRepository)
            {
                _authorsFileHelper.SaveAuthorFileInRepository(_globals.AuthorsFilePath, _globals.GitDir);
            }
            return exitCode;
        }

        private void UpdateLoggerOnDebugging()
        {
            if (_globals.DebugOutput)
            {
                var consoleRule = LogManager.Configuration.LoggingRules.First();
                consoleRule.EnableLoggingForLevel(LogLevel.Debug);
                //consoleRule.DisableLoggingForLevel(LogLevel.Trace);
                LogManager.ReconfigExistingLoggers();
            }
        }

        public int Main(GitTfsCommand command, IList<string> unparsedArgs)
        {
            Trace.WriteLine(_gitTfsVersionProvider.GetVersionString());
            if (_globals.ShowHelp)
            {
                return _help.ShowHelp(command);
            }
            if (_globals.ShowVersion)
            {
                Trace.TraceInformation(_gitTfsVersionProvider.GetVersionString());
                Trace.TraceInformation(GitTfsConstants.MessageForceVersion);
                return GitTfsExitCodes.OK;
            }
            try
            {
                return _runner.Run(command, unparsedArgs);
            }
            finally
            {
                _container.GetInstance<Janitor>().Dispose();
            }
        }

        public bool RequiresValidGitRepository(GitTfsCommand command)
        {
            return !command.GetType().GetCustomAttributes(typeof(RequiresValidGitRepositoryAttribute), false).IsEmpty();
        }

        private void ParseAuthorsAndSave(bool couldSaveAuthorFile)
        {
            try
            {
                _container.GetInstance<AuthorsFile>().Parse(_globals.AuthorsFilePath, _globals.GitDir, couldSaveAuthorFile);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error when parsing author file:" + ex);
                if (!string.IsNullOrEmpty(_globals.AuthorsFilePath))
                    throw;
                Trace.TraceWarning("warning: author file ignored due to a problem occuring when reading it :\n\t" + ex.Message);
                Trace.TraceWarning("         Verify the file :" + Path.Combine(_globals.GitDir, AuthorsFile.GitTfsCachedAuthorsFileName));
            }
        }

        public void InitializeGlobals()
        {
            if (_globals.GitDir != null)
            {
                _globals.GitDirSetByUser = true;
            }
            else
            {
                _globals.GitDir = ".git";
            }
            _globals.Bootstrapper = _bootstrapper;
        }

        public void AssertValidGitRepository()
        {
            var git = _container.GetInstance<IGitHelpers>();
            if (!Directory.Exists(_globals.GitDir))
            {
                if (_globals.GitDirSetByUser)
                {
                    throw new Exception("This command must be run inside a git repository!\nGIT_DIR=" + _globals.GitDir + " explicitly set, but it is not a directory.");
                }
                var gitDir = _globals.GitDir;
                _globals.GitDir = null;
                string cdUp = null;
                git.WrapGitCommandErrors("This command must be run inside a git repository!\nAlready at top level, but " + gitDir + " not found.",
                                         () =>
                                             {
                                                 cdUp = git.CommandOneline("rev-parse", "--show-cdup");
                                                 if (string.IsNullOrEmpty(cdUp))
                                                     gitDir = ".";
                                                 else
                                                     cdUp = cdUp.TrimEnd();
                                                 if (string.IsNullOrEmpty(cdUp))
                                                     cdUp = ".";
                                             });
                Environment.CurrentDirectory = cdUp;
                if (!Directory.Exists(gitDir))
                {
                    throw new Exception("This command must be run inside a git repository!\n" + gitDir + " still not found after going to " + cdUp);
                }
                _globals.GitDir = gitDir;
            }
            _globals.Repository = git.MakeRepository(_globals.GitDir);
        }

        public GitTfsCommand ExtractCommand(IList<string> args)
        {
            for (int i = 0; i < args.Count; i++)
            {
                var command = _commandFactory.GetCommand(args[i]);
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
            return command.GetAllOptions(_container).Parse(args);
        }
    }
}
