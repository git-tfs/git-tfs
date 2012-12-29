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

        public OptionSet OptionSet { get; private set; }

        public Branch(Globals globals, TextWriter stdout)
        {
            this.globals = globals;
            this.stdout = stdout;

            this.OptionSet = globals.OptionSet;
        }

        private class Visitor : IBranchVisitor
        {
            private readonly TextWriter _stdout;
            private readonly string _targetPath;

            public Visitor(string targetPath, TextWriter writer)
            {
                _targetPath = targetPath;
                _stdout = writer;
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

                _stdout.WriteLine();
            }
        }

        public int Run()
        {
            stdout.WriteLine("TFS branches:");
            stdout.WriteLine("");

            var repo = globals.Repository;
            var remote = repo.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId);

            var root = remote.Tfs.GetRootTfsBranchForRemotePath(remote.TfsRepositoryPath);

            var visitor = new Visitor(remote.TfsRepositoryPath, stdout);

            root.AcceptVisitor(visitor);

            stdout.WriteLine("");

            return GitTfsExitCodes.OK;
        }
    }
}