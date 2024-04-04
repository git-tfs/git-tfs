using System.ComponentModel;
using NDesk.Options;
using GitTfs.Core;
using StructureMap;

namespace GitTfs.Commands
{
    [Pluggable("pull")]
    [Description("pull [options]")]
    [RequiresValidGitRepository]
    public class Pull : GitTfsCommand
    {
        private readonly Fetch _fetch;
        private readonly Globals _globals;
        private bool _shouldRebase;

        public OptionSet OptionSet => _fetch.OptionSet
                            .Add("r|rebase", "Rebase your modifications on tfs changes", v => _shouldRebase = v != null);

        public Pull(Globals globals, Fetch fetch)
        {
            _fetch = fetch;
            _globals = globals;
        }

        public int Run() => Run(_globals.RemoteId);

        public int Run(string remoteId)
        {
            var retVal = _fetch.Run(remoteId);

            if (retVal == 0)
            {
                var remote = _globals.Repository.ReadTfsRemote(remoteId);
                if (_shouldRebase)
                {
                    _globals.WarnOnGitVersion();

                    if (_globals.Repository.WorkingCopyHasUnstagedOrUncommitedChanges)
                    {
                        throw new GitTfsException("error: You have local changes; rebase-workflow only possible with clean working directory.")
                            .WithRecommendation("Try 'git stash' to stash your local changes and pull again.");
                    }
                    _globals.Repository.CommandNoisy("rebase", "--rebase-merges", remote.RemoteRef);
                }
                else
                    _globals.Repository.Merge(remote.RemoteRef);
            }

            return retVal;
        }
    }
}
