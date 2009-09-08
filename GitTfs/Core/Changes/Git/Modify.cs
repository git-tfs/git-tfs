using System;

namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Modify : IGitChangedFile
    {
        private readonly string _path;
        private readonly string _newSha;
        private readonly IGitRepository _repository;

        public Modify(IGitRepository repository, string path, string newSha)
        {
            _repository = repository;
            _newSha = newSha;
            _path = path;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            workspace.Edit(_path);
            var workspaceFile = workspace.GetLocalPath(_path);
            _repository.GetBlob(_newSha, workspaceFile);
        }
    }
}
