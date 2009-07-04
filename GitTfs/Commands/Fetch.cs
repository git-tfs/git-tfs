using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("fetch")]
    [Description("fetch [options] [tfs-remote-id]...")]
    public class Fetch : GitTfsCommand
    {
        private readonly FcOptions fcOptions;
        private readonly RemoteOptions remoteOptions;
        private readonly Globals globals;

        public Fetch(FcOptions fcOptions, Globals globals)
        {
            this.fcOptions = fcOptions;
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
        public bool parent { get; set; }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(fcOptions, remoteOptions); }
        }

        public int Run(IList<string> args)
        {
            IEnumerable<GitTfsRemote> remotesToFetch;
            if (parent)
                remotesToFetch = new[] {WorkingHeadInfo("HEAD").GitTfsRemote};
            else if (all)
                remotesToFetch = globals.CurrentRepository.ReadAllTfsRemotes();
            else
            {
                if(args.Count == 0) args = new[] {globals.RemoteId};
                remotesToFetch = args.Select(arg => globals.CurrentRepository.ReadTfsRemote(arg));
            }

            foreach(var remote in remotesToFetch)
            {
                remote.Fetch();
            }
            return 0;
        }

        private TfsCommitMetaInfo WorkingHeadInfo(string head)
        {
            return WorkingHeadInfo(head, new List<string>());
        }

        private TfsCommitMetaInfo WorkingHeadInfo(string head, IList<string> localCommits)
        {
            TfsCommitMetaInfo retVal = null;
            globals.CurrentRepository.CommandOutputPipe(stdout => retVal = ParseFirstTfsCommit(stdout),
              "log", "--no-color", "--first-parent", "--pretty=medium", head);
            return retVal;
        }

        private TfsCommitMetaInfo ParseFirstTfsCommit(TextReader stdout, IList<string> localCommits)
        {
            string currentCommit = null;
            string line;
            var commitRegex = new Regex("commit (" + GitTfsConstants.Sha1 + ")");
            while(null != (line = stdout.ReadLine()))
            {
                var match = commitRegex.Match(line);
                if(match.IsMatch)
                {
                    if(currentCommit != null) localCommits.Add(currentCommit);
                    currentCommit = match.Groups[1].Value;
                    continue;
                }
                var commitInfo = TfsCommitMetaInfo.TryParse(match.Groups[1].Value);
                if(commitInfo != null && currentCommit == commitInfo.Remote.MaxCommitHash)
                    return commitInfo;
            }
            return null;
        }

        class TfsCommitMetaInfo
        {
            public string TfsUrl { get; set; }
            public string TfsSourcePath { get; set; }
            public long ChangesetId { get; set; }
            public GitTfsRemote Remote { get; set; } // Need to expand this to do a dynamic lookup & cache

            // e.g. git-tfs-id: [http://team:8080/]$/sandbox;C123
            private static readonly Regex tfsInfoRegex =
                new Regex("^\\s*" +
                          "git-tfs-id:\\s+" +
                          "\\[(.+)\\]" +
                          "(.+);" +
                          "C(\\d+)" +
                          "\\s*$");

            public static TfsCommitMetaInfo TryParse(string gitTfsMetaInfo)
            {
                match = tfsInfoRegex.Match(line);
                if(match.IsMatch)
                {
                    var commitInfo = ObjectFactory.GetInstance<TfsCommitMetaInfo>();
                    commitInfo.TfsUrl = match.Groups[1].Value;
                    commitInfo.TfsSourcePath = match.Groups[2].Value;
                    commitInfo.ChangesetId = Convert.ToInt32(match.Groups[3].Value);
                    return commitInfo;
                }
                return null;
            }
        }
    }
}
