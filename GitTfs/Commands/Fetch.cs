using System.Collections.Generic;
using System.ComponentModel;
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
        public bool parent { get; set; }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(fcOptions, remoteOptions); }
        }

        public int Run(IList<string> args)
        {
            IEnumerable<GitTfsRemote> remotesToFetch;
            if (parent)
                remotesToFetch = new[] {WorkingHeadInfo("HEAD").Remote};
            else if (all)
                remotesToFetch = globals.Repository.ReadAllTfsRemotes();
            else
            {
                if(args.Count == 0) args = new[] {globals.RemoteId};
                remotesToFetch = args.Select(arg => globals.Repository.ReadTfsRemote(arg));
            }

            foreach(var remote in remotesToFetch)
            {
                remote.Fetch();
            }
            return 0;
        }

        private TfsChangesetInfo WorkingHeadInfo(string head)
        {
            return WorkingHeadInfo(head, new List<string>());
        }

        private TfsChangesetInfo WorkingHeadInfo(string head, ICollection<string> localCommits)
        {
            TfsChangesetInfo retVal = null;
            globals.Repository.CommandOutputPipe(stdout => retVal = ParseFirstTfsCommit(stdout, localCommits),
              "log", "--no-color", "--first-parent", "--pretty=medium", head);
            return retVal;
        }

        private TfsChangesetInfo ParseFirstTfsCommit(Stream stdout, ICollection<string> localCommits)
        {
            return ParseFirstTfsCommit(new StreamReader(stdout), localCommits);
        }

        private TfsChangesetInfo ParseFirstTfsCommit(TextReader stdout, ICollection<string> localCommits)
        {
            string currentCommit = null;
            string line;
            var commitRegex = new Regex("commit (" + GitTfsConstants.Sha1 + ")");
            while(null != (line = stdout.ReadLine()))
            {
                var match = commitRegex.Match(line);
                if(match.Success)
                {
                    if(currentCommit != null) localCommits.Add(currentCommit);
                    currentCommit = match.Groups[1].Value;
                    continue;
                }
                var commitInfo = TfsChangesetInfo.TryParse(match.Groups[1].Value);
                if(commitInfo != null && currentCommit == commitInfo.Remote.MaxCommitHash)
                    return commitInfo;
            }
            return null;
        }
    }
}
