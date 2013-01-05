using System.ComponentModel;
using System.Diagnostics;
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
        private readonly Help helper;
        private readonly Cleanup cleanup;
        public bool DisplayRemotes { get; set; }
        public bool RenameRemote { get; set; }
        public bool DeleteRemote { get; set; }

        public OptionSet OptionSet
        {
            get { 
                return new OptionSet
                {
                    { "r|remotes", "Display all the TFS branch of the current TFS server", v => DisplayRemotes = (v != null) },
                    { "m|move", "Rename a TFS branch", v => RenameRemote = (v != null) },
                    { "delete", "Delete a TFS branch", v => DeleteRemote = (v != null) },
                }
                .Merge(globals.OptionSet); 
            }
        }

        public Branch(Globals globals, TextWriter stdout, Help helper, Cleanup cleanup)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.helper = helper;
            this.cleanup = cleanup;
        }

        public int Run(string oldRemoteName, string newRemoteName)
        {
            if (!RenameRemote)
            {
                helper.Run(this);
                return GitTfsExitCodes.Help;
            }

            var newRemoteNameExpected = globals.Repository.AssertValidBranchName(newRemoteName.ToGitRefName());
            if (newRemoteNameExpected != newRemoteName)
                stdout.WriteLine("The name of the branch after renaming will be : " + newRemoteNameExpected);

            if (globals.Repository.HasRemote(newRemoteNameExpected))
            {
                throw new GitTfsException("error: this remote name is already used!");
            }

            stdout.WriteLine("Cleaning before processing rename...");
            cleanup.Run();

            globals.Repository.MoveRemote(oldRemoteName, newRemoteNameExpected);

            if(globals.Repository.RenameBranch(oldRemoteName, newRemoteName) == null)
                stdout.WriteLine("warning: no local branch found to rename");

            return GitTfsExitCodes.OK;
        }

        public int Run(string remoteName)
        {
            if (!DeleteRemote)
            {
                helper.Run(this);
                return GitTfsExitCodes.Help;
            }

            var remote = globals.Repository.ReadTfsRemote(remoteName);
            if (remote == null)
            {
                throw new GitTfsException(string.Format("Error: Remote \"{0}\" not found!", remoteName));
            }

            stdout.WriteLine("Cleaning before processing delete...");
            cleanup.Run();

            globals.Repository.DeleteTfsRemote(remote);
            return GitTfsExitCodes.OK;
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
