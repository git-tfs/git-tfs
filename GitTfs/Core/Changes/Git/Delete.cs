namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Delete : IGitChangedFile
    {
        private readonly string _path;

        public Delete(string path)
        {
            _path = path;
        }

        public void Apply(ITfsWorkspace workspace)
        {
            workspace.Delete(_path);
        }
    }
}
