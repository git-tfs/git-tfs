namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Add : IGitChangedFile
    {
        private readonly string _path;
        private readonly IGitRepository _repository;
        private readonly string _newSha;

        public Add(IGitRepository repository, string path, string newSha)
        {
            _repository = repository;
            _path = path;
            _newSha = newSha;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            var workspaceFile = workspace.GetLocalPath(_path);
            _repository.GetBlob(_newSha, workspaceFile);
            workspace.Add(_path);
        }
    }
}
