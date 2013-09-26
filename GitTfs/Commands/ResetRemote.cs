using System;
using System.ComponentModel;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("reset-remote")]
    [Description("reset-remote commit-sha1-ref\n ex : git tfs reset-remote 3dcce821d7a20e6b2499cdd6f2f52ffbe8507be7")]
    [RequiresValidGitRepository]
    public class ResetRemote : GitTfsCommand
    {
        private readonly Globals globals;

        public ResetRemote(Globals globals)
        {
            this.globals = globals;
        }

        public virtual OptionSet OptionSet { get { return new OptionSet { }; } }

        public int Run(string commitRef)
        {
            var currentTfsCommit = globals.Repository.GetCurrentTfsCommit();
            if (currentTfsCommit == null)
                throw new GitTfsException("error : the current commit does not belong to a tfs remote!");

            var targetCommit = globals.Repository.GetTfsCommit(commitRef);
            if (targetCommit == null)
                throw new GitTfsException("error : the commit where you want to reset the tfs remote \""
                + currentTfsCommit.Remote.Id + "\" does not belong to a tfs remote!");

            if (targetCommit.Remote.Id != currentTfsCommit.Remote.Id)
                throw new GitTfsException("error : the commit where you want to reset the tfs remote \""
                + currentTfsCommit.Remote.Id + "\" does not belong to the same tfs remote!");

            globals.Repository.ResetRemote(currentTfsCommit.Remote, targetCommit.GitCommit);

            return GitTfsExitCodes.OK;
        }
    }
}
