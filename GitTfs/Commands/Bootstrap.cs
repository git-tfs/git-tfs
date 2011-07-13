using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("bootstrap")]
    [RequiresValidGitRepository]
    [Description("bootstrap [parent-commit]")]
    public class Bootstrap : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly TextWriter _stdout;

        public Bootstrap(Globals globals, TextWriter stdout)
        {
            _globals = globals;
            _stdout = stdout;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(); }
        }

        public int Run()
        {
            return Run("HEAD");
        }

        public int Run(string commitish)
        {
            var tfsParents = _globals.Repository.GetParentTfsCommits(commitish, true);
            foreach (var parent in tfsParents)
            {
                _globals.Repository.CommandNoisy("log", "-1", parent.GitCommit);
                if (parent.Remote.IsDerived)
                {
                    var remoteId = GetRemoteId(parent);
                    _globals.Repository.CreateTfsRemote(remoteId, parent);
                    _stdout.WriteLine("-> new remote " + remoteId);
                }
                else
                {
                    _stdout.WriteLine("-> existing remote " + parent.Remote.Id);
                }
                _stdout.WriteLine();
            }
            return GitTfsExitCodes.OK;
        }

        private string GetRemoteId(TfsChangesetInfo parent)
        {
            if (IsAvailable(GitTfsConstants.DefaultRepositoryId))
                return GitTfsConstants.DefaultRepositoryId;

            var hostname = new Uri(parent.Remote.TfsUrl).Host.Replace(".", "-");
            var remoteId = hostname;
            var suffix = 0;
            while (!IsAvailable(remoteId))
                remoteId = hostname + "-" + (suffix++);
            return remoteId;
        }

        private bool IsAvailable(string remoteName)
        {
            return !_globals.Repository.HasRemote(remoteName);
        }
    }
}
