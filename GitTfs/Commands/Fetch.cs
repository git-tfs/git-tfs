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
        private readonly AuthorsFile authors;
        private readonly Labels labels;

        public Fetch(Globals globals, TextWriter stdout, RemoteOptions remoteOptions, AuthorsFile authors, Labels labels)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.remoteOptions = remoteOptions;
            this.authors = authors;
            this.labels = labels;
        }

        bool FetchAll { get; set; }
        bool FetchLabels { get; set; }
        bool FetchParents { get; set; }
        string BareBranch { get; set; }
        bool ForceFetch { get; set; }
        bool ExportMetadatas { get; set; }
        string ExportMetadatasFile { get; set; }
        public bool IgnoreBranches { get; set; }
        private int _batchSize;
        public int BatchSize { get { return _batchSize; } }
        public string BatchSizeOption
        {
            set
            {
                if (!int.TryParse(value, out _batchSize))
                    throw new GitTfsException("error: batch size parameter should be an integer.");
            }
        }

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
                    { "ignore-branches", "Ignore fetching merged branches when encounter merge changesets",
                        v => IgnoreBranches = v != null },
                    { "batch-size=", "Size of a the batch of tfs changesets fetched (-1 for all in one batch)",
                        v => BatchSizeOption = v },
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
            if (!FetchAll && IgnoreBranches)
                globals.Repository.SetConfig(GitTfsConstants.IgnoreBranches, true.ToString());

            foreach (var remote in GetRemotesToFetch(args))
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
            if (remote.Repository.IsBare)
            {
                if(string.IsNullOrEmpty(BareBranch))
                    throw new GitTfsException("error : specify a git branch to fetch on...");
                if (!remote.Repository.HasRef(GitRepository.ShortToLocalName(BareBranch)))
                    throw new GitTfsException("error : the specified git branch doesn't exist...");
                if (!ForceFetch && remote.MaxCommitHash != remote.Repository.GetCommit(BareBranch).Sha)
                    throw new GitTfsException("error : fetch is not allowed when there is ahead commits!",
                        new List<string>() {"Remove ahead commits and retry", "use the --force option (ahead commits will be lost!)"});
            }

            // It is possible that we have outdated refs/remotes/tfs/<id>.
            // E.g. someone already fetched changesets from TFS into another git repository and we've pulled it since
            // in that case tfs fetch will retrieve same changes again unnecessarily. To prevent it we will scan tree from HEAD and see if newer changesets from
            // TFS exists (by checking git-tfs-id mark in commit's comments).
            // The process is similar to bootstrapping.
            if (!ForceFetch)
                globals.Repository.MoveTfsRefForwardIfNeeded(remote);
            var exportMetadatasFilePath = Path.Combine(globals.GitDir, "git-tfs_workitem_mapping.txt");
            if (ExportMetadatas)
            {
                remote.ExportMetadatas = true;
                remote.Repository.SetConfig(GitTfsConstants.ExportMetadatasConfigKey, "true");
                if (!string.IsNullOrEmpty(ExportMetadatasFile))
                {
                    if (File.Exists(ExportMetadatasFile))
                    {
                        File.Copy(ExportMetadatasFile, exportMetadatasFilePath);
                    }
                    else
                        throw new GitTfsException("error: the work items mapping file doesn't exist!");
                }
            }
            else
            {
                if(remote.Repository.GetConfig(GitTfsConstants.ExportMetadatasConfigKey) == "true")
                    remote.ExportMetadatas = true;
            }
            remote.ExportWorkitemsMapping = new Dictionary<string, string>();
            if (remote.ExportMetadatas && File.Exists(exportMetadatasFilePath))
            {
                try
                {
                    foreach (var lineRead in File.ReadAllLines(exportMetadatasFilePath))
                    {
                        if (string.IsNullOrWhiteSpace(lineRead))
                            continue;
                        var values = lineRead.Split('|');
                        var oldWorkitem = values[0].Trim();
                        if(!remote.ExportWorkitemsMapping.ContainsKey(oldWorkitem))
                            remote.ExportWorkitemsMapping.Add(oldWorkitem, values[1].Trim());
                    }
                }
                catch (Exception)
                {
                    throw new GitTfsException("error: bad format of workitems mapping file! One line format should be: OldWorkItemId|NewWorkItemId");
                }
            }

            try
            {
                remote.Fetch(stopOnFailMergeCommit);

            }
            finally
            {
                Trace.WriteLine("Cleaning...");
                remote.CleanupWorkspaceDirectory();

                if (remote.Repository.IsBare)
                    remote.Repository.UpdateRef(GitRepository.ShortToLocalName(BareBranch), remote.MaxCommitHash);
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
}
