using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("branch")]
    [Description("branch")]
    [RequiresValidGitRepository]
    public class Branch : GitTfsCommand
    {
        private Globals globals;
        private TextWriter stdout;
        public bool DisplayRemotes { get; set; }

        public OptionSet OptionSet
        {
            get { 
                return new OptionSet
                {
                    { "r|remotes", "Display all the TFS branch of the current TFS server", v => DisplayRemotes = (v != null) }
                }
                .Merge(globals.OptionSet); 
            }
        }

        public Branch(Globals globals, TextWriter stdout)
        {
            this.globals = globals;
            this.stdout = stdout;
        }

        private class Visitor : IBranchVisitor
        {
            private readonly TextWriter _stdout;
            private readonly string _targetPath;
            private readonly IEnumerable<IGitTfsRemote> _tfsRemotes;

            public Visitor(string targetPath, TextWriter writer, IEnumerable<IGitTfsRemote> tfsRemotes = null)
            {
                _targetPath = targetPath;
                _stdout = writer;
                _tfsRemotes = tfsRemotes;
            }

            public void Visit(IBranch branch, int level)
            {
                for (var i = 0; i < level-1; i++ )
                    _stdout.Write(" | ");

                if (level > 0)
                    _stdout.Write(" +- ");

                _stdout.Write(branch.Path);

                if (branch.Path.Equals(_targetPath))
                    _stdout.Write(" * ");

                if (_tfsRemotes != null)
                {
                    var remote = _tfsRemotes.FirstOrDefault(r => r.TfsRepositoryPath == branch.Path);
                    if (remote != null)
                        _stdout.Write("  -> " + remote.Id);
                }

                _stdout.WriteLine();
            }
        }

        public int Run()
        {
            var tfsRemotes = globals.Repository.ReadAllTfsRemotes();
            if (DisplayRemotes)
            {
                stdout.WriteLine("TFS branches:");
                stdout.WriteLine("");

                var repo = globals.Repository;
                var remote = repo.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);

                var root = remote.Tfs.GetRootTfsBranchForRemotePath(remote.TfsRepositoryPath);

                var visitor = new Visitor(remote.TfsRepositoryPath, stdout, tfsRemotes);

                root.AcceptVisitor(visitor);

                stdout.WriteLine("");

                return GitTfsExitCodes.OK;
            }

            stdout.WriteLine("Git-tfs remotes:");
            foreach (var remote in tfsRemotes)
            {
                stdout.WriteLine("\n {0} -> {1} {2}", remote.Id, remote.TfsUrl, remote.TfsRepositoryPath);
                stdout.WriteLine("        {0} - {1} @ {2}", remote.RemoteRef, remote.MaxCommitHash, remote.MaxChangesetId);
            }
            return GitTfsExitCodes.OK;
        }
    }
}