
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

        void IGitChangedFile.Apply(ITfsWorkspaceCopy workspace)
        {
            workspace.Edit(Path, NewSha);
            workspace.Rename(Path, PathTo);
        }
    }
}