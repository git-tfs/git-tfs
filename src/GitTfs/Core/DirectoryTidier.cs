using GitTfs.Core.TfsInterop;

namespace GitTfs.Core
{
    public class DirectoryTidier : ITfsWorkspaceModifier, IDisposable
    {
        private enum FileOperation
        {
            Add,
            Remove,
            RenameFrom,
            RenameTo,
            Edit,
            EditAndRenameFrom,
        }

        private readonly ITfsWorkspaceModifier _workspace;
        private readonly Func<IEnumerable<TfsTreeEntry>> _getInitialTfsTree;
        private List<string> _filesInTfs;
        private readonly Dictionary<string, FileOperation> _fileOperations;
        private bool _disposed;

        public DirectoryTidier(ITfsWorkspaceModifier workspace, Func<IEnumerable<TfsTreeEntry>> getInitialTfsTree)
        {
            _workspace = workspace;
            _getInitialTfsTree = getInitialTfsTree;
            _fileOperations = new Dictionary<string, FileOperation>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            var candidateDirectories = CalculateCandidateDirectories();
            if (!candidateDirectories.Any())
                return;

            _filesInTfs = _getInitialTfsTree().Where(entry => entry.Item.ItemType == TfsItemType.File).Select(entry => entry.FullName.ToLowerInvariant()).ToList();

            foreach (var fileAndOperation in _fileOperations)
            {
                if (fileAndOperation.Value == FileOperation.Remove)
                    _filesInTfs.Remove(fileAndOperation.Key.ToLowerInvariant());
                else if (fileAndOperation.Value == FileOperation.Add || fileAndOperation.Value == FileOperation.RenameTo)
                    _filesInTfs.Add(fileAndOperation.Key.ToLowerInvariant());
            }

            var deletedDirs = new List<string>();
            foreach (var dir in candidateDirectories.OrderBy(d => d, StringComparer.InvariantCultureIgnoreCase))
            {
                DeleteEmptyDir(dir, deletedDirs);
            }
        }

        private void DeleteEmptyDir(string dirName, List<string> deletedDirs)
        {
            if (dirName == null)
                return;
            var downcasedDirName = dirName.ToLowerInvariant();
            if (!HasEntryInDir(downcasedDirName))
            {
                DeleteEmptyDir(GetDirectoryName(dirName), deletedDirs);
                if (!IsDirDeletedAlready(downcasedDirName, deletedDirs))
                {
                    _workspace.Delete(dirName);
                    deletedDirs.Add(downcasedDirName);
                }
            }
        }

        private bool IsDirDeletedAlready(string downcasedDirName, IEnumerable<string> deletedDirs) => deletedDirs.Any(t => downcasedDirName.StartsWith(t + "/") || t == downcasedDirName);

        private string GetDirectoryName(string path)
        {
            var separatorIndex = path.LastIndexOf('/');
            if (separatorIndex == -1)
                return null;
            return path.Substring(0, separatorIndex);
        }

        private bool HasEntryInDir(string dirName)
        {
            dirName = dirName + "/";
            return _filesInTfs.Any(file => file.StartsWith(dirName));
        }

        string ITfsWorkspaceModifier.GetLocalPath(string path) => _workspace.GetLocalPath(path);

        void ITfsWorkspaceModifier.Add(string path)
        {
            _workspace.Add(path);
            _fileOperations.Add(path, FileOperation.Add);
        }

        void ITfsWorkspaceModifier.Edit(string path)
        {
            _workspace.Edit(path);
            _fileOperations.Add(path, FileOperation.Edit);
        }

        void ITfsWorkspaceModifier.Delete(string path)
        {
            _workspace.Delete(path);
            _fileOperations.Add(path, FileOperation.Remove);
        }

        void ITfsWorkspaceModifier.Rename(string pathFrom, string pathTo, string score)
        {
            _workspace.Rename(pathFrom, pathTo, score);

            FileOperation pathFromOperation;
            if (_fileOperations.TryGetValue(pathFrom, out pathFromOperation) &&
                pathFromOperation == FileOperation.Edit)
            {
                _fileOperations[pathFrom] = FileOperation.EditAndRenameFrom;
            }
            else
            {
                _fileOperations.Add(pathFrom, FileOperation.RenameFrom);
            }
            _fileOperations.Add(pathTo, FileOperation.RenameTo);
        }

        private IEnumerable<string> CalculateCandidateDirectories()
        {
            var directoriesWithRemovedFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var removedFilePath in _fileOperations.Where(x => x.Value == FileOperation.Remove).Select(x => x.Key))
            {
                var directory = GetDirectoryName(removedFilePath);
                if (directory != null)
                {
                    directoriesWithRemovedFiles.Add(directory);
                }
            }

            return directoriesWithRemovedFiles;
        }
    }
}
