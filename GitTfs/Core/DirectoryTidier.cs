using System;

namespace Sep.Git.Tfs.Core
{
    public class DirectoryTidier : ITfsWorkspaceModifier, IDisposable
    {
        ITfsWorkspaceModifier _workspace;

        public DirectoryTidier(ITfsWorkspaceModifier workspace, object initialTfsTree)
        {
            _workspace = workspace;
        }

        public void Dispose()
        {
        }

        public string GetLocalPath(string path)
        {
            return _workspace.GetLocalPath(path);
        }

        public void Add(string path)
        {
            _workspace.Add(path);
        }

        public void Edit(string path)
        {
            _workspace.Edit(path);
        }

        public void Delete(string path)
        {
            _workspace.Delete(path);
        }

        public void Rename(string pathFrom, string pathTo, string score)
        {
            _workspace.Rename(pathFrom, pathTo, score);
        }
    }
}
