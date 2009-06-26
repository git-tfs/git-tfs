namespace Sep.Git.Tfs.Commands
{
    [GitTfsCommand("-h")]
    [GitTfsCommand("--help")]
    [GitTfsCommand("help")]
    public class Help : GitTfsCommand
    {
        /// <summary>
        /// Figures out whether it should show help for git-tfs or for
        /// a particular command, and then does that.
        /// </summary>
        public int Run(IEnumerable<string> args)
        {
            foreach(var arg in args)
            {
                var command = container.GetByName(arg);
                if(command is GitTfsCommand)
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
            foreach(var commandName in GetCommandNames().Sort())
            {
                output.WriteLine("    " + commandName);
            }
        }

        private IEnumerable<string> GetCommandNames()
        {
            foreach(var commandType in GetCommandTypes())
            {
                foreach(var commandName in GitTfsCommandAttribute.GetCommandNames(commandType))
                {
                    yield return commandName;
                }
            }
        }

        /// <summary>
        /// Shows help for a specific command.
        /// </summary>
        public int Run(GitTfsCommand command)
        {
        }
    }
}
