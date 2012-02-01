using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap;
using StructureMap.Pipeline;
using StructureMap.Query;
using IContainer = StructureMap.IContainer;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("help")]
    [Description("help [command-name]")]
    public class Help : GitTfsCommand
    {
        private readonly TextWriter output;
        private readonly GitTfsCommandFactory commandFactory;
        private readonly IContainer _container;

        public Help(TextWriter output, GitTfsCommandFactory commandFactory, IContainer container)
        {
            this.output = output;
            this.commandFactory = commandFactory;
            _container = container;
        }

        public OptionSet OptionSet
        {
            get { return new OptionSet(); }
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
                else
                {
                    output.WriteLine("Invalid argument: " + arg);
                }
            }
            return Run();
        }

        /// <summary>
        /// Shows help for git-tfs as a whole (i.e. a list of commands).
        /// </summary>
        public int Run()
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

            output.WriteLine("Usage: git-tfs " + GetCommandUsage(command));
            command.GetAllOptions(_container).WriteOptionDescriptions(output);

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

        private string GetCommandName(GitTfsCommand command)
        {
            return (from instance in GetCommandInstances()
                    where instance.ConcreteType == command.GetType()
                    select instance.Name).Single();
        }

        private IEnumerable<InstanceRef> GetCommandInstances()
        {
            return _container.Model
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
    }

    public interface IHelpHelper
    {
        int ShowHelp(GitTfsCommand command);
        int ShowHelpForInvalidArguments(GitTfsCommand command);
    }

    public class HelpHelper : IHelpHelper
    {
        private readonly IContainer _container;

        public HelpHelper(IContainer container)
        {
            _container = container;
        }

        public int ShowHelp(GitTfsCommand command)
        {
            return _container.GetInstance<Help>().Run(command);
        }

        public int ShowHelpForInvalidArguments(GitTfsCommand command)
        {
            ShowHelp(command);
            return GitTfsExitCodes.InvalidArguments;
        }
    }
}
