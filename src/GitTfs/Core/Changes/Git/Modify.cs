﻿namespace GitTfs.Core.Changes.Git
{
    public class Modify : IGitChangedFile
    {
        public string Path { get; private set; }
        public string NewSha { get; private set; }
        public IGitRepository _repository { get; private set; }

        public Modify(IGitRepository repository, GitChangeInfo changeInfo)
        {
            _repository = repository;
            NewSha = changeInfo.newSha;
            Path = changeInfo.path;
        }

        public void Apply(ITfsWorkspaceModifier workspace)
        {
            workspace.Edit(Path);
            var workspaceFile = workspace.GetLocalPath(Path);
            _repository.CopyBlob(NewSha, workspaceFile);
        }
    }
}
