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

        private int Run(string commitish)
        {
            var tfsParents = _globals.Repository.GetParentTfsCommits(commitish);
            foreach (var parent in tfsParents)
            {
                _stdout.WriteLine("Parent found: " + parent.ChangesetId);
                _stdout.WriteLine("   -- " + parent.Remote.Id + ", " + parent.Remote.Tfs.Url);
                var remoteName = GetRemoteName(parent);
                _stdout.WriteLine("   -> new remote " + remoteName);
                _globals.Repository.CreateTfsRemote(remoteName, parent.Remote.Tfs.Url, parent.Remote.TfsRepositoryPath, null);
            }
            return GitTfsExitCodes.OK;
        }

        private string GetRemoteName(TfsChangesetInfo parent)
        {
            var defaultRemoteName = "default";
            if (IsAvailable(defaultRemoteName))
                return defaultRemoteName;
            var hostname = new Uri(parent.Remote.Tfs.Url).Host.Replace(".", "-");
            var remoteName = hostname;
            var suffix = 0;
            while (!IsAvailable(remoteName))
                remoteName = hostname + "-" + (suffix++);
            return hostname;
        }

        private bool IsAvailable(string remoteName)
        {
            return !_globals.Repository.HasRemote(remoteName);
        }
    }
}
