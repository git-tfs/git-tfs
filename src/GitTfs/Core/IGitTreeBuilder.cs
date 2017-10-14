using LibGit2Sharp;

namespace GitTfs.Core
{
    public interface IGitTreeModifier
    {
        void Add(string path, string file, LibGit2Sharp.Mode mode);
        void Remove(string path);
    }

    public interface IGitTreeBuilder : IGitTreeModifier
    {
        Tree GetTree();
    }
}
