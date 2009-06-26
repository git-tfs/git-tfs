namespace Sep.Git.Tfs
{
    public interface GitTfsCommand
    {
        int Run(IEnumerable<string> args);
    }
}
