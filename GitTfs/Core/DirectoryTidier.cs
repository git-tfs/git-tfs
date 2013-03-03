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
        List<string> _filesRenamedInTfs;
        bool _disposed;

        public DirectoryTidier(ITfsWorkspaceModifier workspace, IEnumerable<TfsTreeEntry> initialTfsTree)
        {
            _workspace = workspace;
            _filesInTfs = initialTfsTree.Where(entry => entry.Item.ItemType == TfsItemType.File).Select(entry => entry.FullName.ToLowerInvariant()).ToList();
            _filesRemovedFromTfs = new List<string>();
            _filesRenamedInTfs = new List<string>();
        }


        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            var deletedDirs = new List<string>();
            //TFS don't permit to delete an empty directory when the directory was emptied by moving outside a file (i.e. renaming)
            //Until the changeset is done, tfs consider the file still belongs to the folder :( 
            var cantDeleteDirsWithCase = _filesRenamedInTfs.Select(f => GetDirectoryName(f)).Distinct().ToList();

            var cantDeleteDirs = cantDeleteDirsWithCase.Select(f => f.ToLowerInvariant()).Distinct().ToList();

            foreach (var removedFile in _filesRemovedFromTfs)
            {
                DeleteEmptyDir(GetDirectoryName(removedFile), deletedDirs, cantDeleteDirs);
            }

            //TODO : we have here the list of the directories that we should delete in the future...
            var shouldDeleteDirectoriesAfter = cantDeleteDirsWithCase.Where(d => !HasEntryInDir(d));
        }

        void DeleteEmptyDir(string dirName, List<string> deletedDirs, List<string> cantDeleteDirs)
        {
            if (dirName == null)
                return;
            var downcasedDirName = dirName.ToLowerInvariant();
            if (!HasEntryInDir(downcasedDirName) && !deletedDirs.Contains(downcasedDirName) && !cantDeleteDirs.Contains(downcasedDirName))
            {
                _workspace.Delete(dirName);
                deletedDirs.Add(downcasedDirName);
                DeleteEmptyDir(GetDirectoryName(dirName), deletedDirs, cantDeleteDirs);
            }
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


        public string GetLocalPath(string path)
        {
            return _workspace.GetLocalPath(path);
        }

        public void Add(string path)
        {
            _workspace.Add(path);
            _filesInTfs.Add(path.ToLowerInvariant());
        }

        public void Edit(string path)
        {
            _workspace.Edit(path);
        }

        public void Delete(string path)
        {
            _workspace.Delete(path);
            _filesRemovedFromTfs.Add(path);
            _filesInTfs.Remove(path.ToLowerInvariant());
        }

        public void Rename(string pathFrom, string pathTo, string score)
        {
            _workspace.Rename(pathFrom, pathTo, score);
            _filesRenamedInTfs.Add(pathFrom);
            _filesInTfs.Remove(pathFrom.ToLowerInvariant());
            _filesInTfs.Add(pathTo.ToLowerInvariant());
        }
    }
}
