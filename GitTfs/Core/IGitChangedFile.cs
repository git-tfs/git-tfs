namespace Sep.Git.Tfs.Core
{
    public interface IGitChangedFile
    {
        void Apply(ITfsWorkspace workspace);
    }
}