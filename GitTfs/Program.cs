namespace Sep.Git.Tfs
{
    public class GitTfs
    {
        public static void Main(string [] args)
        {
            Initialize();
            Main(new List<string>(args));
        }

        private static void Main(List<string> args)
        {
            var command = ExtractCommand(args) ?? GetHelp();
            var unparsedArgs = ParseOptions(command, args);
            Environment.ExitCode = command.Run(unparsedArgs);
        }

        private static void Initialize()
        {
            // do stuff with the container
        }

        private GitTfsCommand ExtractCommand(List<string> args)
        {
            for(int i = 0; i < args.Length; i++)
            {
                var command = container.GetByName(args[i]) as GitTfsCommand;
                if(command != null)
                {
                    args.RemoveAt(i);
                    return command;
                }
            }
        }
    }
}
