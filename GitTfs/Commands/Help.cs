using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("help")]
    [Description("help [command-name]")]
    public class Help : GitTfsCommand
    {
        private readonly TextWriter output;

        public Help(TextWriter output)
        {
            this.output = output;
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
                var command = ObjectFactory.TryGetInstance<GitTfsCommand>(arg);
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
            foreach(var commandName in GetCommandNames().OrderBy(s => s))
            {
                output.WriteLine("    " + commandName);
            }
            output.WriteLine(" (use 'git-tfs help [command]' for more information)");
            return GitTfsExitCodes.Help;
        }

        private IEnumerable<string> GetCommandNames()
        {
            foreach(var commandType in GetCommandTypes())
            {
                var name = GetCommandName(commandType);
                if(name != null)
                    yield return name;
            }
        }

        private string GetCommandName(Type commandType)
        {
            var attribute = (PluggableAttribute) commandType.GetCustomAttributes(typeof (PluggableAttribute), false).FirstOrDefault();
            return attribute == null ? null : attribute.ConcreteKey;
        }

        private string GetCommandName(object obj)
        {
            return GetCommandName(obj.GetType());
        }

        private IEnumerable<Type> GetCommandTypes()
        {
            var commandType = typeof (GitTfsCommand);
            return from t in GetType().Assembly.GetTypes()
                   where commandType.IsAssignableFrom(t)
                   select t;
        }

        private string GetCommandUsage(GitTfsCommand command)
        {
            var descriptionAttribute =
                command.GetType().GetCustomAttributes(typeof (DescriptionAttribute), false).FirstOrDefault() as
                DescriptionAttribute;
            return (descriptionAttribute != null)
                       ? descriptionAttribute.Description
                       : GetCommandName(command) + " [options]";
        }

        /// <summary>
        /// Shows help for a specific command.
        /// </summary>
        public int Run(GitTfsCommand command)
        {
            if(command is Help)
                return Run();

            var usage = new UsageBuilder();
            usage.BeginSection("where options are:");
            foreach(var parseHelper in Program.GetOptionParseHelpers(command))
                usage.AddOptions(parseHelper);
            usage.EndSection();
            output.WriteLine("Usage: git-tfs " + GetCommandUsage(command));
            usage.ToText(output, OptStyle.Unix, true);
            return GitTfsExitCodes.Help;
        }
    }
}
