using LibGit2Sharp;

namespace GitTfs.Core
{
    public class GitTreeBuilder : IGitTreeBuilder
    {
        private readonly TreeDefinition _treeDefinition;
        private readonly ObjectDatabase _objectDatabase;
        private readonly string _gitDir;

        public GitTreeBuilder(ObjectDatabase objectDatabase, string gitDir)
        {
            _treeDefinition = new TreeDefinition();
            _objectDatabase = objectDatabase;
            _gitDir = gitDir;
        }

        public GitTreeBuilder(ObjectDatabase objectDatabase, string gitDir, Tree tree)
        {
            _treeDefinition = TreeDefinition.From(tree);
            _objectDatabase = objectDatabase;
            _gitDir = gitDir;
        }

        public void Add(string path, string file, LibGit2Sharp.Mode mode)
        {
            // If this is a root .gitattributes file, copy its content to $GIT_DIR/info/attributes
            // so that libgit2's filter attribute lookup can find it for subsequent blob creations.
            // This is necessary because git-tfs creates commits directly via the object database
            // without updating the working directory or index between commits.
            if (_gitDir != null && string.Equals(path, ".gitattributes", System.StringComparison.OrdinalIgnoreCase))
            {
                var infoDir = System.IO.Path.Combine(_gitDir, "info");
                if (!System.IO.Directory.Exists(infoDir))
                    System.IO.Directory.CreateDirectory(infoDir);
                System.IO.File.Copy(file, System.IO.Path.Combine(infoDir, "attributes"), true);
            }

            using (var stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var blob = _objectDatabase.CreateBlob(stream, path);
                _treeDefinition.Add(path, blob, mode);
            }
        }

        public void Remove(string path) => _treeDefinition.Remove(path);

        public Tree GetTree() => _objectDatabase.CreateTree(_treeDefinition);
    }
}
