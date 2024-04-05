using System.ComponentModel;
using NDesk.Options;
using GitTfs.Core;
using StructureMap;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("reset-remote")]
    [Description("reset-remote commit-sha1-ref\n ex : git tfs reset-remote 3dcce821d7a20e6b2499cdd6f2f52ffbe8507be7")]
    [RequiresValidGitRepository]
    public class ResetRemote : GitTfsCommand
    {
        private readonly Globals _globals;
        private bool ForceResetRemote;

        public ResetRemote(Globals globals)
        {
            _globals = globals;
        }

        public virtual OptionSet OptionSet => new OptionSet
                    {
                        { "force", "Force reset remote (when current commit do not belong to the remote to reset)", v => ForceResetRemote = v != null },
                    };

        public int Run(string commitRef)
        {
            var targetCommit = _globals.Repository.GetTfsCommit(commitRef);
            if (targetCommit == null)
                throw new GitTfsException("error : the commit where you want to reset the tfs remote does not belong to a tfs remote!");

            if (!ForceResetRemote)
            {
                var currentTfsCommit = _globals.Repository.GetCurrentTfsCommit();
                if (currentTfsCommit == null)
                    throw new GitTfsException("error : the current commit does not belong to a tfs remote!",
                        new List<string> { "Use '--force' option to reset a remote from a commit not belonging a tfs remote" });

                if (targetCommit.Remote.Id != currentTfsCommit.Remote.Id)
                    throw new GitTfsException("error : the commit where you want to reset the tfs remote does not belong to the current tfs remote \""
                                              + currentTfsCommit.Remote.Id + "\"!",
                        new List<string> { "Use '--force' option to reset the remote \""
                                              + targetCommit.Remote.Id + "\" to a commit not belonging to the current remote \""
                                              + currentTfsCommit.Remote.Id + "\"" });
            }

            _globals.Repository.ResetRemote(targetCommit.Remote, targetCommit.GitCommit);

            Trace.TraceInformation("Remote 'tfs/" + targetCommit.Remote.Id + "' reset successfully.\n");
            Trace.TraceInformation("Note: remember to use the '--force' option when doing the next 'fetch' to force git-tfs to fetch again the changesets!");
            return GitTfsExitCodes.OK;
        }
    }
}
