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
    [Description("branch\n       git tfs branch -r\n       git tfs branch -r -all\n       git tfs branch $/Repository/ProjectBranchToCreate <myWishedRemoteName>\n       git tfs branch --move oldTfsRemoteName newTfsRemoteName\n       git tfs branch --delete tfsRemoteName\n")]
    [RequiresValidGitRepository]
    public class Branch : GitTfsCommand
    {
        private Globals globals;
        private TextWriter stdout;
        private readonly Help helper;
        private readonly Cleanup cleanup;
        private readonly InitBranch initBranch;
        public bool DisplayRemotes { get; set; }
        public bool DisplayAllRootRemotes { get; set; }
        public bool ShouldRenameRemote { get; set; }
        public bool ShouldDeleteRemote { get; set; }
        public string Comment { get; set; }

        public OptionSet OptionSet
        {
            get { 
                return new OptionSet
                {
                    { "r|remotes", "Display the TFS branches of the current TFS root branch existing on the TFS server", v => DisplayRemotes = (v != null) },
                    { "a|all", "Display the TFS branches of all the root branches existing on the TFS server", v => DisplayAllRootRemotes = (v != null) },
                    { "comment=", "Comment used for the creation of the TFS branch ", v => Comment = v },
                    { "m|move", "Rename a TFS remote", v => ShouldRenameRemote = (v != null) },
                    { "delete", "Delete a TFS remote", v => ShouldDeleteRemote = (v != null) },
                }
                .Merge(globals.OptionSet); 
            }
        }

        public Branch(Globals globals, TextWriter stdout, Help helper, Cleanup cleanup, InitBranch initBranch)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.helper = helper;
            this.cleanup = cleanup;
            this.initBranch = initBranch;
        }

        {
        public int Run()
        {
            if (ShouldRenameRemote || ShouldDeleteRemote)
                return helper.Run(this);

            return DisplayBranchData();
        }

        public int Run(string param)
        {
            if (ShouldRenameRemote)
                return helper.Run(this);

            {
            }

            if (ShouldDeleteRemote)
                return DeleteRemote(param);

            return CreateRemote(param);
        }

        public int Run(string param1, string param2)
        {

            if (ShouldDeleteRemote)
                return helper.Run(this);

            if (ShouldRenameRemote)
                return RenameRemote(param1, param2);

            return CreateRemote(param1, param2);
        }

        private int RenameRemote(string oldRemoteName, string newRemoteName)
        {
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

        private int CreateRemote(string tfsPath, string gitBranchNameExpected = null)
        {
            tfsPath.AssertValidTfsPath();
            Trace.WriteLine("Getting commit informations...");
            var commit = globals.Repository.GetCurrentTfsCommit();
            if(commit == null)
                throw new GitTfsException("error : the current commit is not checked in TFS!");
            var remote = commit.Remote;
            Trace.WriteLine("Creating branch in TFS...");
            remote.Tfs.CreateBranch(remote.TfsRepositoryPath, tfsPath, (int)commit.ChangesetId, Comment ?? "Creation branch " + tfsPath);
            Trace.WriteLine("Init branch in local repository...");
            return initBranch.Run(tfsPath, gitBranchNameExpected);
        }

        private int DeleteRemote(string remoteName)
        {
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

        public int DisplayBranchData()
        {
            // should probably pull this from options so that it is settable from the command-line
            const string remoteId = GitTfsConstants.DefaultRepositoryId;

            var tfsRemotes = globals.Repository.ReadAllTfsRemotes();
            if (DisplayRemotes)
            {
                if (!DisplayAllRootRemotes)
                {
                    var remote = globals.Repository.ReadTfsRemote(remoteId);

                    stdout.WriteLine("\nTFS branch structure:");
                    WriteRemoteTfsBranchStructure(remote.Tfs, stdout, remote.TfsRepositoryPath, tfsRemotes);
                    return GitTfsExitCodes.OK;
                }
                else
                {
                    var remote = tfsRemotes.First(r => r.Id == remoteId);
                    if (!remote.Tfs.CanGetBranchInformation)
                    {
                        throw new GitTfsException("error: this version of TFS doesn't support this functionality");
                    }
                    foreach (var branch in remote.Tfs.GetBranches().Where(b=>b.IsRoot))
                    {
                        var root = remote.Tfs.GetRootTfsBranchForRemotePath(branch.Path);
                        var visitor = new WriteBranchStructureTreeVisitor(remote.TfsRepositoryPath, stdout, tfsRemotes);
                        root.AcceptVisitor(visitor);
                    }
                    return GitTfsExitCodes.OK;
                }
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
