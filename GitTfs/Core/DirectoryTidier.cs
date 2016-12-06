using System;
using System.Collections.Generic;
using System.Linq;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class DirectoryTidier : ITfsWorkspaceCopy, IDisposable
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
        private readonly IGitRepository _reprository;
        private readonly Func<IEnumerable<TfsTreeEntry>> _getInitialTfsTree;
        private List<string> _filesInTfs;
        private readonly Dictionary<string, FileOperation> _fileOperations;
        private readonly Dictionary<string, string> _renameOperations;
        private readonly Dictionary<string, string> _editOperations;
        private bool _disposed;

        public DirectoryTidier(ITfsWorkspaceModifier workspace, IGitRepository repository, Func<IEnumerable<TfsTreeEntry>> getInitialTfsTree)
        {
            _workspace = workspace;
            _reprository = repository;
            _getInitialTfsTree = getInitialTfsTree;
            _fileOperations = new Dictionary<string, FileOperation>(StringComparer.InvariantCultureIgnoreCase);
            _renameOperations = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            _editOperations = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            foreach(var fileOperation in _fileOperations)
            {
                switch (fileOperation.Value)
                {
                    case FileOperation.Add:
                        _workspace.Add(fileOperation.Key);
                        break;
                    case FileOperation.Edit:
                           _workspace.Edit(fileOperation.Key);
                        break;
                    case FileOperation.Remove:
                         _workspace.Delete(fileOperation.Key);
                        break;
                    case FileOperation.RenameTo:
                         _workspace.Rename(fileOperation.Key, _renameOperations[fileOperation.Key]);
                        break;

                    case FileOperation.EditAndRenameFrom:
                        case FileOperation.RenameFrom:
                        break;
                }
            }

            foreach(var editOperation in _editOperations)
            {
                var file = GetLocalPath(editOperation.Key);
                if (_reprository != null)
                {
                    _reprository.CopyBlob(editOperation.Value, file);
                }
            }

            var candidateDirectories = CalculateCandidateDirectories();
            if (!candidateDirectories.Any())
                return;

            _filesInTfs = _getInitialTfsTree().Where(entry => entry.Item.ItemType == TfsItemType.File).Select(entry => entry.FullName.ToLowerInvariant()).ToList();

            foreach (var fileAndOperation in _fileOperations)
            {
                if (fileAndOperation.Value == FileOperation.Remove)
                    _filesInTfs.Remove(fileAndOperation.Key.ToLowerInvariant());
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

        private bool IsDirDeletedAlready(string downcasedDirName, IEnumerable<string> deletedDirs)
        {
            return deletedDirs.Any(t => downcasedDirName.StartsWith(t + "/") || t == downcasedDirName);
        }

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

        string GetLocalPath(string path)
        {
            return _workspace.GetLocalPath(path);
        }

        void ITfsWorkspaceCopy.Add(string path, string shaFile)
        {
            if (!_fileOperations.ContainsKey(path))
            {
                _fileOperations.Add(path, FileOperation.Add);
            }
            else
            {
                //git rename is split into add/delete with only case difference
                if (_fileOperations[path] == FileOperation.Remove)
                {
                    _fileOperations[path] = FileOperation.Edit;
                }
                else
                {
                    throw new ApplicationException("Unable to add item " + path + " it was already modified.");
                }
            }
            _editOperations.Add(path, shaFile);
        }

        void ITfsWorkspaceCopy.Edit(string path, string shaFile)
        {
            FileOperation pathFromOperation;
            if (_fileOperations.TryGetValue(path, out pathFromOperation) &&
                pathFromOperation == FileOperation.RenameFrom)
            {
                _fileOperations[path] = FileOperation.EditAndRenameFrom;
            }
            else
            {
                _fileOperations.Add(path, FileOperation.Edit);
            }
            _editOperations.Add(path, shaFile);
        }

        void ITfsWorkspaceCopy.Delete(string path)
        {
            if (!_fileOperations.ContainsKey(path))
            {
                _fileOperations.Add(path, FileOperation.Remove);
            }
            else
            {
                //git rename is split into add/delete with only case difference
                if (_fileOperations[path] == FileOperation.Remove)
                {
                    _fileOperations[path] = FileOperation.Edit;
                }
                else
                {
                    throw new ApplicationException("Unable to delete item " + path + " it was already modified.");
                }
            }
        }

        void ITfsWorkspaceCopy.Rename(string pathFrom, string pathTo)
        {
            if (pathFrom.ToLower().CompareTo(pathTo.ToLower()) != 0)
            {
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
                _renameOperations.Add(pathTo, pathFrom);
            }
            else
            {
                //git rename with only case changes is impossible to transfer to tfs
            }
        }

        private IEnumerable<string> CalculateCandidateDirectories()
        {
            var directoriesWithRemovedFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var directoriesBlockedForRemoval = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var fileAndOperation in _fileOperations)
            {
                var directory = GetDirectoryName(fileAndOperation.Key);
                switch (fileAndOperation.Value)
                {
                    case FileOperation.Remove:
                        directoriesWithRemovedFiles.Add(directory);
                        break;
                    default:
                        directoriesBlockedForRemoval.Add(directory);
                        break;
                }
            }

            directoriesBlockedForRemoval.Add(null);
            directoriesWithRemovedFiles.ExceptWith(directoriesBlockedForRemoval);
            return directoriesWithRemovedFiles;
        }
    }
}
