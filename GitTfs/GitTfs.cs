using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StructureMap;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs
{
    public class GitTfs
    {
        private IGitTfsVersionProvider _gitTfsVersionProvider;
        private ITfsHelper tfsHelper;
        private GitTfsCommandFactory commandFactory;
        private readonly IHelpHelper _help;
        private readonly IContainer _container;
        private readonly GitTfsCommandRunner _runner;
        private readonly Globals _globals;

        public GitTfs(ITfsHelper tfsHelper, GitTfsCommandFactory commandFactory, IHelpHelper help, IContainer container, IGitTfsVersionProvider gitTfsVersionProvider, GitTfsCommandRunner runner, Globals globals)
        {
            this.tfsHelper = tfsHelper;
            this.commandFactory = commandFactory;
            _help = help;
            _container = container;
            _gitTfsVersionProvider = gitTfsVersionProvider;
            _runner = runner;
            _globals = globals;
        }

        public int Run(IList<string> args)
        {
            InitializeGlobals();
            var command = ExtractCommand(args);
            if(RequiresValidGitRepository(command)) AssertValidGitRepository();
            var unparsedArgs = ParseOptions(command, args);
            return Main(command, unparsedArgs);
        }

        public int Main(GitTfsCommand command, IList<string> unparsedArgs)
        {
            Trace.WriteLine(_gitTfsVersionProvider.GetVersionString());
            if(_globals.ShowHelp)
            {
                return _help.ShowHelp(command);
            }
            else if(_globals.ShowVersion)
            {
                _container.GetInstance<TextWriter>().WriteLine(_gitTfsVersionProvider.GetVersionString());
                return GitTfsExitCodes.OK;
            }
            else
            {
                return _runner.Run(command, unparsedArgs);
            }
        }

        public bool RequiresValidGitRepository(GitTfsCommand command)
        {
            return ! command.GetType().GetCustomAttributes(typeof (RequiresValidGitRepositoryAttribute), false).IsEmpty();
        }

        public void InitializeGlobals()
        {
            var git = _container.GetInstance<IGitHelpers>();
            try
            {
                _globals.StartingRepositorySubDir = git.CommandOneline("rev-parse", "--show-prefix");
            }
            catch (Exception)
            {
                _globals.StartingRepositorySubDir = "";
            }
            if(_globals.GitDir != null)
            {
                _globals.GitDirSetByUser = true;
            }
            else
            {
                _globals.GitDir = ".git";
            }
            _globals.RemoteId = GitTfsConstants.DefaultRepositoryId;
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
            return command.GetAllOptions(_container).Parse(args);
        }
    }
}
