
namespace GitTfs.Core.Changes.Git
{
    public class Delete : IGitChangedFile
    {
        public string Path { get; private set; }

        public Delete(GitChangeInfo changeInfo)
        {
            Path = changeInfo.path;
        }

        public void Apply(ITfsWorkspaceModifier workspace) => workspace.Delete(Path);
    }
}
