using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("fetch")]
    [Description("fetch [options] [tfs-remote-id]...")]
    [RequiresValidGitRepository]
    public class Fetch : GitTfsCommand
    {
        private readonly RemoteOptions remoteOptions;
        private readonly TextWriter stdout;
        private readonly Globals globals;
        private readonly ConfigProperties properties;
        private readonly AuthorsFile authors;
        private readonly Labels labels;

        public Fetch(Globals globals, ConfigProperties properties, TextWriter stdout, RemoteOptions remoteOptions, AuthorsFile authors, Labels labels)
        {
            this.globals = globals;
            this.properties = properties;
            this.stdout = stdout;
            this.remoteOptions = remoteOptions;
            this.authors = authors;
            this.labels = labels;
            this.upToChangeSet = -1;
            BranchStrategy = BranchStrategy = BranchStrategy.Auto;
        }



        bool FetchAll { get; set; }
        bool FetchLabels { get; set; }
        bool FetchParents { get; set; }
        string BareBranch { get; set; }
        bool ForceFetch { get; set; }
        bool ExportMetadatas { get; set; }
        string ExportMetadatasFile { get; set; }
        public BranchStrategy BranchStrategy { get; set; }
        public string BatchSizeOption
        {
            set
            {
                int batchSize;
                if (!int.TryParse(value, out batchSize))
                    throw new GitTfsException("error: batch size parameter should be an integer.");
                properties.BatchSize = batchSize;
            }
        }

        int upToChangeSet { get; set; }
        public string UpToChangeSetOption
        {
            set
            {
                int changesetIdParsed;
                if (!int.TryParse(value, out changesetIdParsed))
                    throw new GitTfsException("error: 'up-to' parameter should be an integer.");
                upToChangeSet = changesetIdParsed;
            }
        }
        
        protected int? InitialChangeset { get; set; }

        public virtual OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "all|fetch-all", "Fetch TFS changesets of all the initialized tfs remotes",
                        v => FetchAll = v != null },
                    { "parents", "Fetch TFS changesets of the parent(s) initialized tfs remotes",
                        v => FetchParents = v != null },
                    { "l|with-labels|fetch-labels", "Fetch the labels also when fetching TFS changesets",
                        v => FetchLabels = v != null },
                    { "b|bare-branch=", "The name of the branch on which the fetch will be done for a bare repository",
                        v => BareBranch = v },
                    { "force", "Force fetch of tfs changesets when there is ahead commits (ahead commits will be lost!)",
                        v => ForceFetch = v != null },
                    { "x|export", "Export metadatas",
                        v => ExportMetadatas = v != null },
                    { "export-work-item-mapping=", "Path to Work-items mapping export file",
                        v => ExportMetadatasFile = v },
                    { "branches=", "Strategy to manage branches:"+
                        Environment.NewLine + "* none: Ignore branches and merge changesets, fetching only the cloned tfs path"+
                        Environment.NewLine + "* auto:(default) Manage the encountered merged changesets and initialize only the merged branches"+
                        Environment.NewLine + "* all: Manage merged changesets and initialize all the branches during the clone",
                        v =>
                        {
                            BranchStrategy branchStrategy;
                            if (Enum.TryParse(v, true, out branchStrategy))
                                BranchStrategy = branchStrategy;
                            else
                                throw new GitTfsException("error: 'branches' parameter should be of value none/auto/all.");
                        } },
                    { "batch-size=", "Size of the batch of tfs changesets fetched (-1 for all in one batch)",
                        v => BatchSizeOption = v },
                    { "c|changeset=", "The changeset to clone from (must be a number)",
                        v => InitialChangeset = Convert.ToInt32(v) },
                    { "t|up-to=", "up-to changeset # (optional, -1 for up to maximum, must be a number, not prefixed with C)", 
                        v => UpToChangeSetOption = v }
                }.Merge(remoteOptions.OptionSet);
            }
        }

        public int Run()
        {
            return Run(globals.RemoteId);
        }

        public void Run(bool stopOnFailMergeCommit)
        {
            Run(stopOnFailMergeCommit, globals.RemoteId);
        }

        public int Run(params string[] args)
        {
            return Run(false, args);
        }

        private int Run(bool stopOnFailMergeCommit, params string[] args)
        {
            if (!FetchAll && BranchStrategy == BranchStrategy.None)
                globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, true.ToString());

            var remotesToFetch = GetRemotesToFetch(args).ToList();
            foreach (var remote in remotesToFetch)
            {
                FetchRemote(stopOnFailMergeCommit, remote);
            }
            return 0;
        }

        private void FetchRemote(bool stopOnFailMergeCommit, IGitTfsRemote remote)
        {
            stdout.WriteLine("Fetching from TFS remote '{0}'...", remote.Id);
            DoFetch(remote, stopOnFailMergeCommit);
            if (labels != null && FetchLabels)
            {
                stdout.WriteLine("Fetching labels from TFS remote '{0}'...", remote.Id);
                labels.Run(remote);
            }
        }

        protected virtual void DoFetch(IGitTfsRemote remote, bool stopOnFailMergeCommit)
        {
            var bareBranch = string.IsNullOrEmpty(BareBranch) ? remote.Id : BareBranch;

            // It is possible that we have outdated refs/remotes/tfs/<id>.
            // E.g. someone already fetched changesets from TFS into another git repository and we've pulled it since
            // in that case tfs fetch will retrieve same changes again unnecessarily. To prevent it we will scan tree from HEAD and see if newer changesets from
            // TFS exists (by checking git-tfs-id mark in commit's comments).
            // The process is similar to bootstrapping.
            if (!ForceFetch)
            {
                if (!remote.Repository.IsBare)
                    remote.Repository.MoveTfsRefForwardIfNeeded(remote);
                else
                    remote.Repository.MoveTfsRefForwardIfNeeded(remote, bareBranch);
            }

            if (!ForceFetch &&
                remote.Repository.IsBare &&
                remote.Repository.HasRef(GitRepository.ShortToLocalName(bareBranch)) &&
                remote.MaxCommitHash != remote.Repository.GetCommit(bareBranch).Sha)
            {
                throw new GitTfsException("error : fetch is not allowed when there is ahead commits!",
                    new[] {"Remove ahead commits and retry", "use the --force option (ahead commits will be lost!)"});
            }

            var metadataExportInitializer = new ExportMetadatasInitializer(globals);
            bool shouldExport = ExportMetadatas || remote.Repository.GetConfig(GitTfsConstants.ExportMetadatasConfigKey) == "true";

            if (ExportMetadatas)
            {
                metadataExportInitializer.InitializeConfig(remote.Repository, ExportMetadatasFile);
            }

            metadataExportInitializer.InitializeRemote(remote, shouldExport);

            try
            {
                if (InitialChangeset.HasValue)
                {
                    properties.InitialChangeset = InitialChangeset.Value;
                    properties.PersistAllOverrides();
                    remote.QuickFetch(InitialChangeset.Value);
                    remote.Fetch(stopOnFailMergeCommit);
                }
                else
                {
                    remote.Fetch(stopOnFailMergeCommit,upToChangeSet);
                }

            }
            finally
            {
                Trace.WriteLine("Cleaning...");
                remote.CleanupWorkspaceDirectory();

                if (remote.Repository.IsBare)
                    remote.Repository.UpdateRef(GitRepository.ShortToLocalName(bareBranch), remote.MaxCommitHash);
            }
        }

        private IEnumerable<IGitTfsRemote> GetRemotesToFetch(IList<string> args)
        {
            IEnumerable<IGitTfsRemote> remotesToFetch;
            if (FetchParents)
                remotesToFetch = globals.Repository.GetLastParentTfsCommits("HEAD").Select(commit => commit.Remote);
            else if (FetchAll)
                remotesToFetch = globals.Repository.ReadAllTfsRemotes();
            else
                remotesToFetch = args.Select(arg => globals.Repository.ReadTfsRemote(arg));
            return remotesToFetch;
        }
    }

    public enum BranchStrategy
    {
        None,
        Auto,
        All
    }
}
