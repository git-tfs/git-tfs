using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Commands;
using StructureMap;

namespace Sep.Git.Tfs
{
    public class GitTfs
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

        private static void Main(List<string> args)
        {
            InitializeGlobals();
            var command = ExtractCommand(args);
            if(command.RequiresValidGitRepository) AssertValidGitRepository();
            var unparsedArgs = ParseOptions(command, args);
            Environment.ExitCode = command.Run(unparsedArgs);
        }

        public static void InitializeGlobals()
        {
            var globals = ObjectFactory.GetInstance<Globals>();
            globals.StartingRepositorySubDir = git.TryCommandOneline("rev-parse", "--show-prefix") ?? "";
            globals.GitDirSetByUser = Environment.Variables.ContainsKey("GIT_DIR");
            globals.GitDir = globals.GitDirSetByUser ? Environment.Variables["GIT_DIR"] : ".git";
            var options = ObjectFactory.GetInstance<FcOptions>();
            options.OutputLevel = 0;
        }

        private static void AssertValidGitRepository()
        {
            if(Environment.Variables.ContainsKey("GIT_DIR"))
            {
                if(!Directory.Exists(Environment.Variables["GIT_DIR"]))
                {
                    throw new Exception("The GIT_DIR environment variable was set (" + Environment.Variables["GIT_DIR"] + "), but was not a directory.");
                }
                GIT_DIR = Environment.Variables["GIT_DIR"];
            }
            else
            {
                var cdup = CommandOneline("rev-parse", "--show-cdup").Trim();
                if(cdup == String.Empty)
                {
                    GIT_DIR = ".git";
                }
                else
                {
                    Directory.ChangeDirectory(cdup);
                    GIT_DIR = ".git";
                }
            }
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
            initializer.Scan(scanner => scanner.TheCallingAssembly());
            initializer.ForRequestedType<TextWriter>().TheDefault.Is.ConstructedBy(() => Console.Out);
        }

        private static IEnumerable<string> ParseOptions(GitTfsCommand command, IEnumerable<string> args)
        {
            foreach(var parseHelper in GetOptionParseHelpers(command))
            foreach(var parser in GetOptionParsers(command))
            {
                var parser = new Parser(parseHelper);
                args = parser.Parse(args.ToArray());
            }
            return args;
            var parser = new Parser(new PropertyFieldParserHelper(optionContainer));
            var argArray = args.ToArray();
        }

        public static IEnumerable<ParserHelper> GetOptionParseHelpers(GitTfsCommand command)
        {
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
