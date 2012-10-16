using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly Globals globals;
        private readonly AuthorsFile authors;

        public Fetch(Globals globals, RemoteOptions remoteOptions, AuthorsFile authors)
        {
            this.remoteOptions = remoteOptions;
            this.globals = globals;
            this.authors = authors;
        }

//        public int? RevisionToFetch { get; set; }

        bool FetchAll { get; set; }
        bool FetchParents { get; set; }
        string AuthorsFilePath { get; set; }
        public virtual OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "all|fetch-all",
                        v => FetchAll = v != null },
                    { "parents",
                        v => FetchParents = v != null },
                    { "authors=", "Path to an Authors file to map TFS users to Git users",
                        v => AuthorsFilePath = v },
//                    { "r|revision=",
//                        v => RevisionToFetch = Convert.ToInt32(v) },
                }.Merge(remoteOptions.OptionSet);
            }
        }

        public int Run()
        {
            return Run(globals.RemoteId);
        }

        public int Run(params string[] args)
        {
            if (!String.IsNullOrWhiteSpace(AuthorsFilePath))
            {
                if (!File.Exists(AuthorsFilePath))
                {
                    throw new GitTfsException("Authors file cannot be found: '" + AuthorsFilePath + "'");
                }
                else
                {
                    using (StreamReader sr = new StreamReader(AuthorsFilePath))
                    {
                        authors.Parse(sr);
                    }
                }
            }
            foreach (var remote in GetRemotesToFetch(args))
            {
                Trace.WriteLine("Fetching from TFS remote " + remote.Id);
                DoFetch(remote);
            }
            return 0;
        }

        protected virtual void DoFetch(IGitTfsRemote remote)
        {
            // It is possible that we have outdated refs/remotes/tfs/<id>.
            // E.g. someone already fetched changesets from TFS into another git repository and we've pulled it since
            // in that case tfs fetch will retrieve same changes again unnecessarily. To prevent it we will scan tree from HEAD and see if newer changesets from
            // TFS exists (by checking git-tfs-id mark in commit's comments).
            // The process is similar to bootstrapping.
            globals.Repository.MoveTfsRefForwardIfNeeded(remote);
            remote.Fetch();
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
