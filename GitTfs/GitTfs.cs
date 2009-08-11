using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs
{
    class GitTfs
    {
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
            var globals = ObjectFactory.GetInstance<Globals>();
            if(globals.ShowHelp)
            {
                Environment.ExitCode = ObjectFactory.GetInstance<Help>().Run(command);
            }
            else if(globals.ShowVersion)
            {
                ObjectFactory.GetInstance<TextWriter>().WriteLine(MakeVersionString());
                Environment.ExitCode = GitTfsExitCodes.OK;
            }
            else
            {
                Environment.ExitCode = command.Run(unparsedArgs);
                //PostFetchCheckout();
            }
        }

        private string MakeVersionString()
        {
            var versionString = "git-tfs version";
            //versionString += " " + GetType().Assembly.GetName().Version;
            versionString += GetGitCommitForVersionString();
            versionString += " (TFS client library " + ObjectFactory.GetInstance<ITfsHelper>().TfsClientLibraryVersion +
                             ")";
            return versionString;
        }

        private string GetGitCommitForVersionString()
        {
            try
            {
                return " (commit " + GetGitCommit().Substring(0, 8) + "...)";
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
            var git = ObjectFactory.GetInstance<IGitHelpers>();
            var globals = ObjectFactory.GetInstance<Globals>();
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
            var globals = ObjectFactory.GetInstance<Globals>();
            var git = ObjectFactory.GetInstance<IGitHelpers>();
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
                var command = ObjectFactory.TryGetInstance<GitTfsCommand>(args[i]);
                if (command != null)
                {
                    args.RemoveAt(i);
                    return command;
                }
            }
            return ObjectFactory.GetInstance<Help>();
        }

        public IList<string> ParseOptions(GitTfsCommand command, IList<string> args)
        {
            foreach(var parseHelper in command.GetOptionParseHelpers())
            {
                var parser = new Parser(parseHelper);
                args = parser.Parse(args.ToArray());
            }
            return args;
        }
    }
}
