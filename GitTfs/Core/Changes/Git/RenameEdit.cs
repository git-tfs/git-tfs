namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class RenameEdit : IGitChangedFile
    {
        public string Path { get; private set; }
        public string PathTo { get; private set; }
        public string NewSha { get; private set; }
        public string Score { get; private set; }
        public IGitRepository _repository { get; private set; }

        public RenameEdit(IGitRepository repository, GitChangeInfo changeInfo)
        {
            _repository = repository;
            NewSha = changeInfo.newSha;
            Path = changeInfo.path;
            PathTo = changeInfo.pathTo;
            Score = changeInfo.score;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            workspace.Edit(Path);
            workspace.Rename(Path, PathTo, Score);
            var workspaceFile = workspace.GetLocalPath(PathTo);
            _repository.CopyBlob(NewSha, workspaceFile);
        }
    }
}