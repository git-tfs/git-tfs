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
            var command = ExtractCommand(args);
            var unparsedArgs = ParseOptions(command, args);
            Environment.ExitCode = command.Run(unparsedArgs);
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

        private static IEnumerable<string> ParseOptions(object optionContainer, IEnumerable<string> args)
        {
            var parser = new Parser(new PropertyFieldParserHelper(optionContainer));
            return parser.Parse(args.ToArray());
        }
    }
}
