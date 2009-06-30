using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Tfs;
using StructureMap;

namespace Sep.Git.Tfs
{
    public class Program
    {
        public static void Main(string [] args)
        {
            try
            {
                Initialize();
                Main(new List<string>(args));
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);
                Environment.ExitCode = -1;
            }
        }

        private static void Main(IList<string> args)
        {
            InitializeGlobals();
            var command = ExtractCommand(args);
            if(RequiresValidGitRepository(command)) AssertValidGitRepository();
            ReadRepositoryConfig(command);
            var unparsedArgs = ParseOptions(command, args);
            var globals = ObjectFactory.GetInstance<Globals>();
            if(globals.ShowHelp)
            {
                Environment.ExitCode = ObjectFactory.GetInstance<Help>().Run(command);
            }
            else if(globals.ShowVersion)
            {
                ObjectFactory.GetInstance<TextWriter>()
                    .WriteLine("git-tfs version " + typeof (Program).Assembly.GetName().Version +
                               " (TFS client library " + ObjectFactory.GetInstance<ITfsHelper>().TfsClientLibraryVersion +
                               ")");
                Environment.ExitCode = GitTfsExitCodes.OK;
            }
            else
            {
                // TODO -- load authors here, if authors file starts being used.
                // TODO -- migrate config data here.
                // TODO -- verify configuration.
                Environment.ExitCode = command.Run(unparsedArgs);
                //PostFetchCheckout();
            }
        }

        private static bool RequiresValidGitRepository(GitTfsCommand command)
        {
            return 0 !=
                   command.GetType().GetCustomAttributes(typeof (RequiresValidGitRepositoryAttribute), false).Length;
        }

        private static void ReadRepositoryConfig(GitTfsCommand command)
        {
            var globals = ObjectFactory.GetInstance<Globals>();
            if(!Directory.Exists(globals.GitDir)) return;
            Console.WriteLine("TODO: set default option values from GIT_DIR/config");
            // TODO - see git-svn.perl, sub read_repo_config, line 1335.
        }

        public static void InitializeGlobals()
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
            globals.RepositoryId = "default";
        }

        private static void AssertValidGitRepository()
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

        private static GitTfsCommand ExtractCommand(IList<string> args)
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

        private static void Initialize()
        {
            ObjectFactory.Initialize(Initialize);
        }

        private static void Initialize(IInitializationExpression initializer)
        {
            initializer.Scan(scan => { scan.WithDefaultConventions(); scan.TheCallingAssembly(); });
            initializer.ForRequestedType<TextWriter>().TheDefault.Is.ConstructedBy(() => Console.Out);
            DoCustomConfiguration(initializer);
        }

        private static void DoCustomConfiguration(IInitializationExpression initializer)
        {
            foreach(var type in typeof(Program).Assembly.GetTypes())
            {
                foreach(ConfiguresStructureMap attribute in type.GetCustomAttributes(typeof(ConfiguresStructureMap), false))
                {
                    attribute.Initialize(initializer, type);
                }
            }
        }

        private static IList<string> ParseOptions(GitTfsCommand command, IList<string> args)
        {
            foreach(var parseHelper in GetOptionParseHelpers(command))
            {
                var parser = new Parser(parseHelper);
                args = parser.Parse(args.ToArray());
            }
            return args;
        }

        public static IEnumerable<IOptionResults> GetOptionParseHelpers(GitTfsCommand command)
        {
            yield return new PropertyFieldParserHelper(ObjectFactory.GetInstance<Globals>());
            yield return new PropertyFieldParserHelper(command);
            if(command.ExtraOptions != null)
            {
                foreach(var parseHelper in command.ExtraOptions)
                {
                    yield return parseHelper;
                }
            }
        }
    }
}
