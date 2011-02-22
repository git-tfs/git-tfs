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
    [Description("bootstrap [parent-commit [id]]")]
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
            if(_globals.Repository.ReadAllTfsRemotes().Any())
            {
                throw new GitTfsException("You already have a TFS remote.")
                    .WithRecommendation("Try choosing a parent git-tfs commit: `git tfs bootstrap <parent-commit>`.");
            }
            foreach (var parent in _globals.Repository.GetParentTfsCommits("HEAD"))
            {
                _stdout.WriteLine("Parent found: " + parent.ChangesetId);
                _stdout.WriteLine("   -- " + parent.Remote.Id + ", " + parent.Remote.Tfs.Url);
            }
            return GitTfsExitCodes.OK;
        }

        public int Run(string commitish)
        {
            return Run(commitish, "default");
        }

        public int Run(string commitish, string tfsId)
        {
            _globals.Repository.re
            if(_globals.Repository.ReadAllTfsRemotes().Any(remote => remote.Id == tfsId))
            {
                throw new GitTfsException("You already have a TFS remote with id \"" + tfsId + "\".")
                    .WithRecommendation("Try using a different ID: `git tfs bootstrap " + commitish + " <id>`.");
            }
            //var commit = _globals.Repository.GetCommit()
            return GitTfsExitCodes.OK;
        }
    }
}
