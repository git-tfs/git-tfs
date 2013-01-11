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

        public int Run()
        {
            // should probably pull this from options so that it is settable from the command-line
            const string remoteId = GitTfsConstants.DefaultRepositoryId;

            var tfsRemotes = globals.Repository.ReadAllTfsRemotes();
            if (DisplayRemotes)
            {
                var remote = globals.Repository.ReadTfsRemote(remoteId);

                stdout.WriteLine("\nTFS branch structure:");
                WriteRemoteTfsBranchStructure(remote.Tfs, stdout, remote.TfsRepositoryPath, tfsRemotes);
                return GitTfsExitCodes.OK;
            }

            WriteTfsRemoteDetails(stdout, tfsRemotes);
            return GitTfsExitCodes.OK;
        }

        public static void WriteRemoteTfsBranchStructure(ITfsHelper tfsHelper, TextWriter writer, string tfsRepositoryPath, IEnumerable<IGitTfsRemote> tfsRemotes = null)
        {
            var root = tfsHelper.GetRootTfsBranchForRemotePath(tfsRepositoryPath);

            if (!tfsHelper.CanGetBranchInformation)
            {
                throw new GitTfsException("error: this version of TFS doesn't support this functionality");
            }
            var visitor = new WriteBranchStructureTreeVisitor(tfsRepositoryPath, writer, tfsRemotes);
            root.AcceptVisitor(visitor);
        }

        private void WriteTfsRemoteDetails(TextWriter writer, IEnumerable<IGitTfsRemote> tfsRemotes)
        {
            writer.WriteLine("\nGit-tfs remote details:");
            foreach (var remote in tfsRemotes)
            {
                writer.WriteLine("\n {0} -> {1} {2}", remote.Id, remote.TfsUrl, remote.TfsRepositoryPath);
                writer.WriteLine("        {0} - {1} @ {2}", remote.RemoteRef, remote.MaxCommitHash, remote.MaxChangesetId);
            }
        }

        private class WriteBranchStructureTreeVisitor : IBranchTreeVisitor
        {
            private readonly TextWriter _stdout;
            private readonly string _targetPath;
            private readonly IEnumerable<IGitTfsRemote> _tfsRemotes;

            public WriteBranchStructureTreeVisitor(string targetPath, TextWriter writer, IEnumerable<IGitTfsRemote> tfsRemotes = null)
            {
                _targetPath = targetPath;
                _stdout = writer;
                _tfsRemotes = tfsRemotes;
            }

            public void Visit(BranchTree branch, int level)
            {
                for (var i = 0; i < level; i++ )
                    _stdout.Write(" | ");
                
                _stdout.WriteLine();

                for (var i = 0; i < level - 1; i++)
                    _stdout.Write(" | ");

                if (level > 0)
                    _stdout.Write(" +-");

                _stdout.Write(" {0}", branch.Path);

                if (_tfsRemotes != null)
                {
                    var remote = _tfsRemotes.FirstOrDefault(r => r.TfsRepositoryPath == branch.Path);
                    if (remote != null)
                        _stdout.Write(" -> " + remote.Id);
                }

                if (branch.Path.Equals(_targetPath))
                    _stdout.Write(" [*]");

                _stdout.WriteLine();
            }
        }
    }
}
