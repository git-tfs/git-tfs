using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("info")]
    [Description("info")]
    [RequiresValidGitRepository]
    public class Info : GitTfsCommand
    {
        readonly Globals _globals;
        readonly TextWriter _stdout;
        readonly IGitTfsVersionProvider _versionProvider;

        public Info(Globals globals, TextWriter stdout, IGitTfsVersionProvider versionProvider)
        {
            _globals = globals;
            _stdout = stdout;
            _versionProvider = versionProvider;
        }

        public OptionSet OptionSet { get { return _globals.OptionSet; } }

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

            _stdout.WriteLine(_globals.GitVersion);
        }

        private void DescribeGitTfs()
        {
            DisplayReadabilityLineJump();
            _stdout.WriteLine(_versionProvider.GetVersionString());
            _stdout.WriteLine(" " + _versionProvider.GetPathToGitTfsExecutable());

            DescribeGitRepository();
        }

        private void DescribeGitRepository()
        {
            try
            {
                var repoDescription = File.ReadAllLines(@".git\description");
                if (repoDescription.Length == 0 || !repoDescription[0].StartsWith("$/"))
                    return;

                DisplayReadabilityLineJump();

                _stdout.WriteLine("cloned from tfs path:" + string.Join(Environment.NewLine, repoDescription));
            }
            catch (Exception)
            {
                Trace.WriteLine("warning: unable to read the repository description!");
            }
        }

        private void DescribeTfsRemotes(IGitTfsRemote remote)
        {
            DisplayReadabilityLineJump();
            _stdout.WriteLine("remote tfs id: '{0}' {1} {2}", remote.Id, remote.TfsUrl, remote.TfsRepositoryPath);
            _stdout.WriteLine("               {0} - {1} @ {2}", remote.RemoteRef, remote.MaxCommitHash, remote.MaxChangesetId);
        }

        private void DisplayReadabilityLineJump()
        {
            _stdout.WriteLine();
        }
    }
}
