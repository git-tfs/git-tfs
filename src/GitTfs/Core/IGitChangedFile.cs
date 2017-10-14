
namespace GitTfs.Core
{
    public interface IGitChangedFile
    {
        void Apply(ITfsWorkspaceModifier workspace);
    }
}