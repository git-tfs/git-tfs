namespace Sep.Git.Tfs.Core
{
    public interface IGitTreeModifier
    {
        void Add(string path, string file, LibGit2Sharp.Mode mode);
        void Remove(string path);
    }

    public interface IGitTreeBuilder : IGitTreeModifier
    {
        string GetTree();
    }
}
