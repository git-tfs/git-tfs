using System.ComponentModel;
using System.Diagnostics;
using NDesk.Options;
using StructureMap;
using GitTfs.Core;

namespace GitTfs.Commands
{
    [Pluggable("info")]
    [Description("info")]
    [RequiresValidGitRepository]
    public class Info : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly IGitTfsVersionProvider _versionProvider;

        public Info(Globals globals, IGitTfsVersionProvider versionProvider)
        {
            _globals = globals;
            _versionProvider = versionProvider;
        }

        public OptionSet OptionSet => _globals.OptionSet;

        public int Run()
        {
            DescribeGit();

            DescribeGitTfs();

            var tfsRemotes = _globals.Repository.ReadAllTfsRemotes();
            foreach (var remote in tfsRemotes)
            {
                DescribeTfsRemotes(remote);
            }

            return GitTfsExitCodes.OK;
        }

        private void DescribeGit()
        {
            DisplayReadabilityLineJump();

            Trace.TraceInformation(_globals.GitVersion);
        }

        private void DescribeGitTfs()
        {
            DisplayReadabilityLineJump();
            Trace.TraceInformation(_versionProvider.GetVersionString());
            Trace.TraceInformation(" " + _versionProvider.GetPathToGitTfsExecutable());

            Trace.TraceInformation(GitTfsConstants.MessageForceVersion);

            DescribeGitRepository();
        }

        private void DescribeGitRepository()
        {
            try
            {
                var repoDescription = File.ReadAllLines(Path.Combine(_globals.GitDir, "description"));
                if (repoDescription.Length == 0 || !repoDescription[0].StartsWith("$/"))
                    return;

                DisplayReadabilityLineJump();

                Trace.TraceInformation("cloned from tfs path:" + string.Join(Environment.NewLine, repoDescription));
            }
            catch (Exception)
            {
                Trace.WriteLine("warning: unable to read the repository description!");
            }
        }

        private void DescribeTfsRemotes(IGitTfsRemote remote)
        {
            DisplayReadabilityLineJump();
            Trace.TraceInformation("remote tfs id: '{0}' {1} {2}", remote.Id, remote.TfsUrl, remote.TfsRepositoryPath);
            Trace.TraceInformation("               {0} - {1} @ {2}", remote.RemoteRef, remote.MaxCommitHash, remote.MaxChangesetId);
        }

        private void DisplayReadabilityLineJump() => Trace.TraceInformation(string.Empty);
    }
}
