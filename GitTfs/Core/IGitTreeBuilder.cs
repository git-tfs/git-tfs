namespace Sep.Git.Tfs.Core
{
    public interface IGitTreeBuilder
    {
        void Add(string path, string file, LibGit2Sharp.Mode mode);
        void Remove(string path);
        string GetTree();
    }
}
