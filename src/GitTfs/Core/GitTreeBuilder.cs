using LibGit2Sharp;

namespace GitTfs.Core
{
    public class GitTreeBuilder : IGitTreeBuilder
    {
        private readonly TreeDefinition _treeDefinition;
        private readonly ObjectDatabase _objectDatabase;

        public GitTreeBuilder(ObjectDatabase objectDatabase)
        {
            _treeDefinition = new TreeDefinition();
            _objectDatabase = objectDatabase;
        }

        public GitTreeBuilder(ObjectDatabase objectDatabase, Tree tree)
        {
            _treeDefinition = TreeDefinition.From(tree);
            _objectDatabase = objectDatabase;
        }

        public void Add(string path, string file, LibGit2Sharp.Mode mode)
            => _treeDefinition.Add(path, file, mode);

        public void Remove(string path) => _treeDefinition.Remove(path);

        public Tree GetTree() => _objectDatabase.CreateTree(_treeDefinition);
    }
}
