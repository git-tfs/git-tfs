﻿
namespace Sep.Git.Tfs.Core.Changes.Git
{
    public class Delete : IGitChangedFile
    {
        public string Path { get; private set; }

        public Delete(GitChangeInfo changeInfo)
        {
            Path = changeInfo.path;
        }

        void IGitChangedFile.Apply(ITfsWorkspaceCopy workspace)
        {
            workspace.Delete(Path);
        }
    }
}
