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

        public void Apply(ITfsWorkspace workspace)
        {
            workspace.Delete(Path);
            var directoryPath = System.IO.Path.GetDirectoryName(Path);
            var files = Directory.GetFiles(directoryPath);
            if (files.Length == 0)
                workspace.Delete(directoryPath);
        }
    }
}
