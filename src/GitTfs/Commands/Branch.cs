using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NDesk.Options;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using StructureMap;

namespace GitTfs.Commands
{
    [Pluggable("branch")]
    [Description("branch\n\n" +
        "       * Display inited remote TFS branches:\n       git tfs branch\n\n" +
        "       * Display remote TFS branches:\n       git tfs branch -r\n       git tfs branch -r -all\n\n" +
        "       * Create a TFS branch from current commit:\n       git tfs branch $/Repository/ProjectBranchToCreate <myWishedRemoteName> --comment=\"Creation of my branch\"\n\n" +
        "       * Rename a remote branch:\n       git tfs branch --move oldTfsRemoteName newTfsRemoteName\n\n" +
        "       * Delete a remote branch:\n       git tfs branch --delete tfsRemoteName\n       git tfs branch --delete --all\n\n" +
        "       * Initialise an existing remote TFS branch:\n       git tfs branch --init $/Repository/ProjectBranch\n       git tfs branch --init $/Repository/ProjectBranch myNewBranch\n       git tfs branch --init --all\n       git tfs branch --init --tfs-parent-branch=$/Repository/ProjectParentBranch $/Repository/ProjectBranch\n")]
    [RequiresValidGitRepository]
    public class Branch : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly Help _helper;
        private readonly Cleanup _cleanup;
        private readonly InitBranch _initBranch;
        private readonly Rcheckin _rcheckin;
        public bool DisplayRemotes { get; set; }
        public bool ManageAll { get; set; }
        public bool ShouldRenameRemote { get; set; }
        public bool ShouldDeleteRemote { get; set; }
        public bool ShouldInitBranch { get; set; }
        public string IgnoreRegex { get; set; }
        public string ExceptRegex { get; set; }
        public bool NoFetch { get; set; }
        public string Comment { get; set; }
        public string TfsUsername { get; set; }
        public string TfsPassword { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "r|remotes", "Display the TFS branches of the current TFS root branch existing on the TFS server", v => DisplayRemotes = (v != null) },
                    { "all", "Display (used with option --remotes) the TFS branches of all the root branches existing on the TFS server\n" +
                    " or Initialize (used with option --init) all existing TFS branches (For TFS 2010 and later)\n" +
                    " or Delete (used with option --delete) all tfs remotes (for example after lfs migration).", v => ManageAll = (v != null) },
                    { "comment=", "Comment used for the creation of the TFS branch ", v => Comment = v },
                    { "m|move", "Rename a TFS remote", v => ShouldRenameRemote = (v != null) },
                    { "delete", "Delete a TFS remote", v => ShouldDeleteRemote = (v != null) },
                    { "init", "Initialize an existing TFS branch", v => ShouldInitBranch = (v != null) },
                    { "ignore-regex=", "A regex of files to ignore", v => IgnoreRegex = v },
                    { "except-regex=", "A regex of exceptions to ignore-regex", v => ExceptRegex = v},
                    { "no-fetch", "Don't fetch changeset for newly initialized branch(es)", v => NoFetch = (v != null) },
                    { "u|username=", "TFS username", v => TfsUsername = v },
                    { "p|password=", "TFS password", v => TfsPassword = v },
                }
                .Merge(_globals.OptionSet);
            }
        }

        public Branch(Globals globals, Help helper, Cleanup cleanup, InitBranch initBranch, Rcheckin rcheckin)
        {
            _globals = globals;
            _helper = helper;
            _cleanup = cleanup;
            _initBranch = initBranch;
            _rcheckin = rcheckin;
        }

        public void SetInitBranchParameters()
        {
            _initBranch.TfsUsername = TfsUsername;
            _initBranch.TfsPassword = TfsPassword;
            _initBranch.CloneAllBranches = ManageAll;
            _initBranch.IgnoreRegex = IgnoreRegex;
            _initBranch.ExceptRegex = ExceptRegex;
            _initBranch.NoFetch = NoFetch;
        }

        public bool IsCommandWellUsed()
        {
            //Verify that some mutual exclusive options are not used together
            return new[] { ShouldDeleteRemote, ShouldInitBranch, ShouldRenameRemote }.Count(b => b) <= 1;
        }

        public int Run()
        {
            if (!IsCommandWellUsed())
                return _helper.Run(this);

            _globals.WarnOnGitVersion();

            VerifyCloneAllRepository();

            if (ShouldRenameRemote)
                return _helper.Run(this);

            if(ShouldDeleteRemote)
            {
                if (!ManageAll)
                    return _helper.Run(this);
                else
                    return DeleteAllRemotes();
            }

            if (ShouldInitBranch)
            {
                SetInitBranchParameters();
                return _initBranch.Run();
            }

            return DisplayBranchData();
        }

        public int Run(string param)
        {
            if (!IsCommandWellUsed())
                return _helper.Run(this);

            VerifyCloneAllRepository();

            _globals.WarnOnGitVersion();

            if (ShouldRenameRemote)
                return _helper.Run(this);

            if (ShouldInitBranch)
            {
                SetInitBranchParameters();
                return _initBranch.Run(param);
            }

            if (ShouldDeleteRemote)
                return DeleteRemote(param);

            return CreateRemote(param);
        }

        public int Run(string param1, string param2)
        {
            if (!IsCommandWellUsed())
                return _helper.Run(this);

            VerifyCloneAllRepository();

            _globals.WarnOnGitVersion();

            if (ShouldDeleteRemote)
                return _helper.Run(this);

            if (ShouldInitBranch)
            {
                SetInitBranchParameters();
                return _initBranch.Run(param1, param2);
            }

            if (ShouldRenameRemote)
                return RenameRemote(param1, param2);

            return CreateRemote(param1, param2);
        }

        private void VerifyCloneAllRepository()
        {
            if (!_globals.Repository.HasRemote(GitTfsConstants.DefaultRepositoryId))
                return;

            if (_globals.Repository.ReadTfsRemote(GitTfsConstants.DefaultRepositoryId).TfsRepositoryPath == GitTfsConstants.TfsRoot)
                throw new GitTfsException("error: you can't use the 'branch' command when you have cloned the whole repository '$/' !");
        }

        private int RenameRemote(string oldRemoteName, string newRemoteName)
        {
            var newRemoteNameExpected = _globals.Repository.AssertValidBranchName(newRemoteName.ToGitRefName());
            if (newRemoteNameExpected != newRemoteName)
                Trace.TraceInformation("The name of the branch after renaming will be : " + newRemoteNameExpected);

            if (_globals.Repository.HasRemote(newRemoteNameExpected))
            {
                throw new GitTfsException("error: this remote name is already used!");
            }

            Trace.TraceInformation("Cleaning before processing rename...");
            _cleanup.Run();

            _globals.Repository.MoveRemote(oldRemoteName, newRemoteNameExpected);

            if (_globals.Repository.RenameBranch(oldRemoteName, newRemoteName) == null)
                Trace.TraceWarning("warning: no local branch found to rename");

            return GitTfsExitCodes.OK;
        }

        private int CreateRemote(string tfsPath, string gitBranchNameExpected = null)
        {
            bool checkInCurrentBranch = false;
            tfsPath.AssertValidTfsPath();
            Trace.WriteLine("Getting commit informations...");
            var commit = _globals.Repository.GetCurrentTfsCommit();
            if (commit == null)
            {
                checkInCurrentBranch = true;
                var parents = _globals.Repository.GetLastParentTfsCommits(_globals.Repository.GetCurrentCommit());
                if (!parents.Any())
                    throw new GitTfsException("error : no tfs remote parent found!");
                commit = parents.First();
            }
            var remote = commit.Remote;
            Trace.WriteLine("Creating branch in TFS...");
            remote.Tfs.CreateBranch(remote.TfsRepositoryPath, tfsPath, commit.ChangesetId, Comment ?? "Creation branch " + tfsPath);
            Trace.WriteLine("Init branch in local repository...");
            _initBranch.DontCreateGitBranch = true;
            var returnCode = _initBranch.Run(tfsPath, gitBranchNameExpected);

            if (returnCode != GitTfsExitCodes.OK || !checkInCurrentBranch)
                return returnCode;

            _rcheckin.RebaseOnto(_initBranch.RemoteCreated.RemoteRef, commit.GitCommit);
            _globals.UserSpecifiedRemoteId = _initBranch.RemoteCreated.Id;
            return _rcheckin.Run();
        }

        private int DeleteRemote(string remoteName)
        {
            var remote = _globals.Repository.ReadTfsRemote(remoteName);
            if (remote == null)
            {
                throw new GitTfsException(string.Format("Error: Remote \"{0}\" not found!", remoteName));
            }

            Trace.TraceInformation("Cleaning before processing delete...");
            _cleanup.Run();

            _globals.Repository.DeleteTfsRemote(remote);
            return GitTfsExitCodes.OK;
        }

        private int DeleteAllRemotes()
        {
            Trace.TraceInformation("Deleting all remotes!!");
            Trace.TraceInformation("Cleaning before processing delete...");
            _cleanup.Run();

            foreach (var remote in _globals.Repository.ReadAllTfsRemotes())
            {
                _globals.Repository.DeleteTfsRemote(remote);
            }
            return GitTfsExitCodes.OK;
        }

        public int DisplayBranchData()
        {
            // should probably pull this from options so that it is settable from the command-line
            const string remoteId = GitTfsConstants.DefaultRepositoryId;

            var tfsRemotes = _globals.Repository.ReadAllTfsRemotes();
            if (DisplayRemotes)
            {
                if (!ManageAll)
                {
                    var remote = _globals.Repository.ReadTfsRemote(remoteId);

                    Trace.TraceInformation("\nTFS branch structure:");
                    WriteRemoteTfsBranchStructure(remote.Tfs, remote.TfsRepositoryPath, tfsRemotes);
                    return GitTfsExitCodes.OK;
                }
                else
                {
                    var remote = tfsRemotes.First(r => r.Id == remoteId);
                    foreach (var branch in remote.Tfs.GetBranches().Where(b => b.IsRoot))
                    {
                        var root = remote.Tfs.GetRootTfsBranchForRemotePath(branch.Path);
                        var visitor = new WriteBranchStructureTreeVisitor(remote.TfsRepositoryPath, tfsRemotes);
                        root.AcceptVisitor(visitor);
                    }
                    return GitTfsExitCodes.OK;
                }
            }

            WriteTfsRemoteDetails(tfsRemotes);
            return GitTfsExitCodes.OK;
        }

        public static void WriteRemoteTfsBranchStructure(ITfsHelper tfsHelper, string tfsRepositoryPath, IEnumerable<IGitTfsRemote> tfsRemotes = null)
        {
            var root = tfsHelper.GetRootTfsBranchForRemotePath(tfsRepositoryPath);
            var visitor = new WriteBranchStructureTreeVisitor(tfsRepositoryPath, tfsRemotes);
            root.AcceptVisitor(visitor);
        }

        private void WriteTfsRemoteDetails(IEnumerable<IGitTfsRemote> tfsRemotes)
        {
            Trace.TraceInformation("\nGit-tfs remote details:");
            foreach (var remote in tfsRemotes)
            {
                Trace.TraceInformation("\n {0} -> {1} {2}", remote.Id, remote.TfsUrl, remote.TfsRepositoryPath);
                Trace.TraceInformation("        {0} - {1} @ {2}", remote.RemoteRef, remote.MaxCommitHash, remote.MaxChangesetId);
            }
        }

        private class WriteBranchStructureTreeVisitor : IBranchTreeVisitor
        {
            private readonly string _targetPath;
            private readonly IEnumerable<IGitTfsRemote> _tfsRemotes;

            public WriteBranchStructureTreeVisitor(string targetPath, IEnumerable<IGitTfsRemote> tfsRemotes = null)
            {
                _targetPath = targetPath;
                _tfsRemotes = tfsRemotes;
            }

            public void Visit(BranchTree branch, int level)
            {
                var writer = new StringWriter();
                for (var i = 0; i < level; i++)
                    writer.Write(" | ");

                writer.WriteLine();

                for (var i = 0; i < level - 1; i++)
                    writer.Write(" | ");

                if (level > 0)
                    writer.Write(" +-");

                writer.Write(" {0}", branch.Path);

                if (_tfsRemotes != null)
                {
                    var remote = _tfsRemotes.FirstOrDefault(r => r.TfsRepositoryPath == branch.Path);
                    if (remote != null)
                        writer.Write(" -> " + remote.Id);
                }

                if (branch.Path.Equals(_targetPath))
                    writer.Write(" [*]");

                Trace.TraceInformation(writer.ToString());
            }
        }
    }
}
