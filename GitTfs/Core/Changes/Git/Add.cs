namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Add : IGitChangedFile
    {
        public string Path { get; protected set; }
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
            Repository.CopyBlob(NewSha, workspaceFile);
            workspace.Add(Path);
        }
    }

    public class Copy : Add
    {
        public Copy(IGitRepository repository, GitChangeInfo changeInfo) : base(repository, changeInfo)
        {
            Path = changeInfo.pathTo;
        }
    }
}
