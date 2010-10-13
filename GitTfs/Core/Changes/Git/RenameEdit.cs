namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class RenameEdit : IGitChangedFile
    {
        private readonly string _path;
        private readonly string _pathTo;
        private readonly string _newSha;
        private readonly IGitRepository _repository;

        public RenameEdit(IGitRepository repository, string path, string pathTo, string newSha)
        {
            _repository = repository;
            _newSha = newSha;
            _path = path;
            _pathTo = pathTo;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            workspace.Edit(_path);
            workspace.Rename(_path, _pathTo);
            var workspaceFile = workspace.GetLocalPath(_pathTo);
            _repository.GetBlob(_newSha, workspaceFile);
        }
    }
}