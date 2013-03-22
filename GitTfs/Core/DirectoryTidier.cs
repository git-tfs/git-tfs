using System;
using System.Collections.Generic;
using System.Linq;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class DirectoryTidier : ITfsWorkspaceModifier, IDisposable
    {
        ITfsWorkspaceModifier _workspace;
        List<string> _filesInTfs;
        List<string> _filesRemovedFromTfs;
        bool _disposed;

        public DirectoryTidier(ITfsWorkspaceModifier workspace, IEnumerable<TfsTreeEntry> initialTfsTree)
        {
            _workspace = workspace;
            _filesInTfs = initialTfsTree.Where(entry => entry.Item.ItemType == TfsItemType.File).Select(entry => entry.FullName.ToLowerInvariant()).ToList();
            _filesRemovedFromTfs = new List<string>();
        }


        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            var deletedDirs = new List<string>();
            foreach (var dir in _filesRemovedFromTfs.Select(f => GetDirectoryName(f)).OrderBy(d => d, StringComparer.InvariantCultureIgnoreCase))
            {
                DeleteEmptyDir(dir, deletedDirs);
            }
        }

        void DeleteEmptyDir(string dirName, List<string> deletedDirs)
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

        bool IsDirDeletedAlready(string downcasedDirName, IEnumerable<string> deletedDirs)
        {
            return deletedDirs.Any(t => downcasedDirName.StartsWith(t + "/") || t == downcasedDirName);
        }

        string GetDirectoryName(string path)
        {
            var separatorIndex = path.LastIndexOf('/');
            if (separatorIndex == -1)
                return null;
            return path.Substring(0, separatorIndex);
        }

        bool HasEntryInDir(string dirName)
        {
            dirName = dirName + "/";
            return _filesInTfs.Any(file => file.StartsWith(dirName));
        }


        string ITfsWorkspaceModifier.GetLocalPath(string path)
        {
            return _workspace.GetLocalPath(path);
        }

        void ITfsWorkspaceModifier.Add(string path)
        {
            _workspace.Add(path);
            _filesInTfs.Add(path.ToLowerInvariant());
        }

        void ITfsWorkspaceModifier.Edit(string path)
        {
            _workspace.Edit(path);
        }

        void ITfsWorkspaceModifier.Delete(string path)
        {
            _workspace.Delete(path);
            _filesRemovedFromTfs.Add(path);
            _filesInTfs.Remove(path.ToLowerInvariant());
        }

        void ITfsWorkspaceModifier.Rename(string pathFrom, string pathTo, string score)
        {
            _workspace.Rename(pathFrom, pathTo, score);
            /*
            // Even though this may have been removed from a directory, we'll
            // make it look like it wasn't, because TFS doesn't allow these
            // directories to be removed.
            // See https://github.com/git-tfs/git-tfs/issues/313
            _filesRemovedFromTfs.Add(pathFrom);
            _filesInTfs.Remove(pathFrom.ToLowerInvariant());
            */
            _filesInTfs.Add(pathTo.ToLowerInvariant());
        }
    }
}
