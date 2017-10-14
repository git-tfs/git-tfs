namespace GitTfs.Core
{
    public interface IGitTfsVersionProvider
    {
        string GetVersionString();

        string GetPathToGitTfsExecutable();
    }
}
