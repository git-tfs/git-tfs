namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Add : IGitChangedFile
    {
        public string Path { get; private set; }
        public IGitRepository Repository { get; private set; }
        public string NewSha { get; private set; }

        public Add(IGitRepository repository, GitChangeInfo changeInfo)
        {
            Repository = repository;
            Path = changeInfo.path;
            NewSha = changeInfo.newSha;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            var workspaceFile = workspace.GetLocalPath(Path);
            Repository.GetBlob(NewSha, workspaceFile);
            workspace.Add(Path);
        }
    }
}
