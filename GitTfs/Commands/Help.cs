using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap;
using StructureMap.Pipeline;
using StructureMap.Query;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("help")]
    [Description("help [command-name]")]
    public class Help : GitTfsCommand
    {
        private readonly TextWriter output;
        private readonly GitTfsCommandFactory commandFactory;

        public Help(TextWriter output, GitTfsCommandFactory commandFactory)
        {
            this.output = output;
            this.commandFactory = commandFactory;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return null; }
        }

        /// <summary>
        /// Figures out whether it should show help for git-tfs or for
        /// a particular command, and then does that.
        /// </summary>
        public int Run(IList<string> args)
        {
            foreach(var arg in args)
            {
                var command = commandFactory.GetCommand(arg);
                if(command != null)
                {
                    return Run(command);
                }
            }
            return Run();
        }

        /// <summary>
        /// Shows help for git-tfs as a whole (i.e. a list of commands).
        /// </summary>
        private int Run()
        {
            output.WriteLine("Usage: git-tfs [command] [options]");
            foreach(var pair in GetCommandMap())
            {
                output.Write("    " + pair.Key);
                
                if (pair.Value.Any())
                {
                    output.WriteLine(" (" + string.Join(", ", pair.Value) + ")");
                }
                else
                {
                    output.WriteLine();
                }
            }
            output.WriteLine(" (use 'git-tfs help [command]' for more information)");
            return GitTfsExitCodes.Help;
        }

        /// <summary>
        /// Shows help for a specific command.
        /// </summary>
        public int Run(GitTfsCommand command)
        {
            if (command is Help)
                return Run();

            var usage = new UsageBuilder();
            usage.BeginSection("where options are:");
            foreach (var parseHelper in command.GetOptionParseHelpers())
                usage.AddOptions(parseHelper);
            usage.EndSection();
            output.WriteLine("Usage: git-tfs " + GetCommandUsage(command));
            usage.ToText(output, OptStyle.Unix, true);
            return GitTfsExitCodes.Help;
        }

        private Dictionary<string, IEnumerable<string>> GetCommandMap()
        {
            return (from instance in GetCommandInstances()
                    where instance.Name != null
                    orderby instance.Name
                    select instance.Name)
                .ToDictionary(s => s, s => commandFactory.GetAliasesForCommandName(s));
        }

        private static string GetCommandName(GitTfsCommand command)
        {
            return (from instance in GetCommandInstances()
                    where instance.ConcreteType == command.GetType()
                    select instance.Name).Single();
        }

        private static IEnumerable<InstanceRef> GetCommandInstances()
        {
            return ObjectFactory.Model
                .PluginTypes
                .Single(p => p.PluginType == typeof (GitTfsCommand))
                .Instances
                .Where(i => i != null);
        }

        private string GetCommandUsage(GitTfsCommand command)
        {
            var descriptionAttribute =
                command.GetType().GetCustomAttributes(typeof (DescriptionAttribute), false).FirstOrDefault() as
                DescriptionAttribute;
            var commandName = GetCommandName(command);
            return (descriptionAttribute != null)
                       ? descriptionAttribute.Description
                       : commandName + " [options]";
        }

        public static int ShowHelp(GitTfsCommand command)
        {
            return ObjectFactory.GetInstance<Help>().Run(command);
        }

        public static int ShowHelpForInvalidArguments(GitTfsCommand command)
        {
            ShowHelp(command);
            return GitTfsExitCodes.InvalidArguments;
        }
    }
}
