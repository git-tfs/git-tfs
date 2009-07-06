using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        public string GitDir { get; set; }
        public string WorkingCopyPath { get; set; }
        public string WorkingCopySubdir { get; set; }

        protected override Process Start(string [] command, Action<ProcessStartInfo> initialize)
        {
            return base.Start(command, initialize.And(SetUpPaths));
        }

        private void SetUpPaths(ProcessStartInfo gitCommand)
        {
            if(GitDir != null)
                gitCommand.EnvironmentVariables["GIT_DIR"] = GitDir;
            if(WorkingCopyPath != null)
                gitCommand.WorkingDirectory = WorkingCopyPath;
            if(WorkingCopySubdir != null)
                gitCommand.WorkingDirectory = Path.Combine(gitCommand.WorkingDirectory, WorkingCopySubdir);
        }

        public IList<string> ReadAllRemotes()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<GitTfsRemote> ReadAllTfsRemotes()
        {
            throw new System.NotImplementedException();
        }

        public GitTfsRemote ReadTfsRemote(string remoteId)
        {
            throw new System.NotImplementedException();
        }
    }

}
