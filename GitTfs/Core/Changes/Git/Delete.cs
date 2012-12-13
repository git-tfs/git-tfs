using System.IO;
namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Delete : IGitChangedFile
    {
        public string Path { get; private set; }

        public Delete(GitChangeInfo changeInfo)
        {
            Path = changeInfo.path;
        }

        public Delete(string path)
        {
            Path = path;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            workspace.Delete(Path);
        }
    }
}
