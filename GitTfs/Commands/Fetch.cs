using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("fetch")]
    [Description("fetch [options] [tfs-remote-id]...")]
    [RequiresValidGitRepository]
    public class Fetch : GitTfsCommand
    {
        private readonly FcOptions fcOptions;
        private readonly RemoteOptions remoteOptions;
        private readonly Globals globals;

        public Fetch(Globals globals, RemoteOptions remoteOptions, FcOptions fcOptions)
        {
            this.fcOptions = fcOptions;
            this.remoteOptions = remoteOptions;
            this.globals = globals;
        }

//        [OptDef(OptValType.ValueReq)]
//        [ShortOptionName('r')]
//        public int? revision { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("fetch-all")]
        public bool all { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('p')]
        public bool parents { get; set; }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(fcOptions, remoteOptions); }
        }

        public int Run()
        {
            return Run(globals.RemoteId);
        }

        public int Run(params string[] args)
        {
            foreach(var remote in GetRemotesToFetch(args))
            {
                Trace.WriteLine("Fetching from TFS remote " + remote.Id);
                DoFetch(remote);
            }
            return 0;
        }

        protected virtual void DoFetch(IGitTfsRemote remote)
        {
            remote.Fetch();
        }

        private IEnumerable<IGitTfsRemote> GetRemotesToFetch(IList<string> args)
        {
            IEnumerable<IGitTfsRemote> remotesToFetch;
            if (parents)
                remotesToFetch = globals.Repository.GetParentTfsCommits("HEAD").Select(commit => commit.Remote);
            else if (all)
                remotesToFetch = globals.Repository.ReadAllTfsRemotes();
            else
                remotesToFetch = args.Select(arg => globals.Repository.ReadTfsRemote(arg));
            return remotesToFetch;
        }
    }
}
