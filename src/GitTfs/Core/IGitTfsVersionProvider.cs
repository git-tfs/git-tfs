namespace Sep.Git.Tfs.Core
{
    public interface IGitTfsVersionProvider
    {
        string GetVersionString();

        string GetPathToGitTfsExecutable();
    }
}
