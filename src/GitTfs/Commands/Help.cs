using System.ComponentModel;
using NDesk.Options;
using GitTfs.Core;
using GitTfs.Util;
using StructureMap;
using StructureMap.Query;
using IContainer = StructureMap.IContainer;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("help")]
    [Description("help [command-name]")]
    public class Help : GitTfsCommand
    {
        private readonly GitTfsCommandFactory _commandFactory;
        private readonly IContainer _container;

        public Help(GitTfsCommandFactory commandFactory, IContainer container)
        {
            _commandFactory = commandFactory;
            _container = container;
        }

        public OptionSet OptionSet => new OptionSet();

        /// <summary>
        /// Figures out whether it should show help for git-tfs or for
        /// a particular command, and then does that.
        /// </summary>
        public int Run(IList<string> args)
        {
            foreach (var arg in args)
            {
                var command = _commandFactory.GetCommand(arg);
                if (command != null)
                {
                    return Run(command);
                }
                else
                {
                    Trace.TraceInformation("Invalid argument: " + arg);
                }
            }
            return Run();
        }

        /// <summary>
        /// Shows help for git-tfs as a whole (i.e. a list of commands).
        /// </summary>
        public int Run()
        {
            Trace.TraceInformation("Usage: git-tfs [command] [options]");
            foreach (var pair in GetCommandMap())
            {
                var command = "    " + pair.Key;

                if (pair.Value.Any())
                {
                    command += " (" + string.Join(", ", pair.Value) + ")";
                }
                Trace.TraceInformation(command);
            }
            Trace.TraceInformation(" (use 'git-tfs help [command]' or 'git-tfs [command] --help' for more information)");
            Trace.TraceInformation("\nFind more help in our online help : https://github.com/git-tfs/git-tfs");
            return GitTfsExitCodes.Help;
        }

        /// <summary>
        /// Shows help for a specific command.
        /// </summary>
        public int Run(GitTfsCommand command)
        {
            if (command is Help)
                return Run();

            Trace.TraceInformation("Usage: git-tfs " + GetCommandUsage(command));
            var writer = new StringWriter();
            command.GetAllOptions(_container).WriteOptionDescriptions(writer);
            Trace.TraceInformation(writer.ToString());

            Trace.TraceInformation("\nFind more help in our online help : https://github.com/git-tfs/git-tfs/blob/master/doc/commands/" + GetCommandName(command) + ".md");

            return GitTfsExitCodes.Help;
        }

        private Dictionary<string, IEnumerable<string>> GetCommandMap() => (from instance in GetCommandInstances()
                                                                            where instance.Name != null
                                                                            orderby instance.Name
                                                                            select instance.Name)
                .ToDictionary(s => s, s => _commandFactory.GetAliasesForCommandName(s));

        private string GetCommandName(GitTfsCommand command) => (from instance in GetCommandInstances()
                                                                 where instance.ConcreteType == command.GetType()
                                                                 select instance.Name).Single();

        private IEnumerable<InstanceRef> GetCommandInstances() => _container.Model
                .PluginTypes
                .Single(p => p.PluginType == typeof(GitTfsCommand))
                .Instances
                .Where(i => i != null);

        private string GetCommandUsage(GitTfsCommand command)
        {
            var descriptionAttribute =
                command.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as
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

        public int ShowHelp(GitTfsCommand command) => _container.GetInstance<Help>().Run(command);

        public int ShowHelpForInvalidArguments(GitTfsCommand command)
        {
            ShowHelp(command);
            return GitTfsExitCodes.InvalidArguments;
        }
    }
}
