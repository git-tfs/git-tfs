using System.ComponentModel;
using NDesk.Options;
using GitTfs.Core;
using StructureMap;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("bootstrap")]
    [RequiresValidGitRepository]
    [Description("bootstrap [parent-commit]\n" +
        " info: if none of your tfs remote exists, always checkout and bootstrap your main remote first.\n")]
    public class Bootstrap : GitTfsCommand
    {
        private readonly RemoteOptions _remoteOptions;
        private readonly Globals _globals;
        private readonly Bootstrapper _bootstrapper;

        public Bootstrap(RemoteOptions remoteOptions, Globals globals, Bootstrapper bootstrapper)
        {
            _remoteOptions = remoteOptions;
            _globals = globals;
            _bootstrapper = bootstrapper;
        }

        public OptionSet OptionSet => _remoteOptions.OptionSet;

        public int Run() => Run("HEAD");

        public int Run(string commitish)
        {
            var tfsParents = _globals.Repository.GetLastParentTfsCommits(commitish);
            foreach (var parent in tfsParents)
            {
                GitCommit commit = _globals.Repository.GetCommit(parent.GitCommit);
                Trace.TraceInformation("commit {0}\nAuthor: {1} <{2}>\nDate:   {3}\n\n    {4}",
                    commit.Sha,
                    commit.AuthorAndEmail.Item1, commit.AuthorAndEmail.Item2,
                    commit.When.ToString("ddd MMM d HH:mm:ss zzz"),
                    commit.Message.Replace("\n", "\n    ").TrimEnd(' '));
                _bootstrapper.CreateRemote(parent);
                Trace.TraceInformation(string.Empty);
            }
            return GitTfsExitCodes.OK;
        }
    }
}
