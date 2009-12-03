using System.Collections.Generic;
using CommandLine.OptParse;

namespace Sep.Git.Tfs.Commands
{
    public class Checkin : GitTfsCommand
    {
        /*
        [OptDef(OptValType.Flag)]
        [ShortOptionName('m')]
        [ShortOptionName('M')]
        public bool merge { get; set }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('s')]
        public string strategy { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('v')]
        public bool verbose { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('n')]
        [LongOptionName("dry-run")]
        [UseNameAsLongOptionName(false)]
        public bool DryRun { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("fetch-all")]
        public bool all { get; set; }

        //public string CommitUrl { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('r')]
        public int revision { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-rebase")]
        [UseNameAsLongOptionName(false)]
        public bool NoRebase { get; set; }

        private CommitOptions commitOptions;
        private FcOptions fcOptions;
        private RemoteOptions remoteOptions;
        */


        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { throw new System.NotImplementedException(); }
        }

        public int Run(IList<string> args)
        {
            throw new System.NotImplementedException();
            // Outline of git svn dcommit (rev 4f2b15ce88b70dd9e269517a9903864393ca873b):
            // 463:     Ensure index is clean.
            // 466:     assume HEAD is what we want to send to SVN, if no head is specified. Set $head to the thing to dcommit.
            // 470-477: set $old_head to current head so we can put it back later.
            // 478:     checkout $head.
            // 482:     Get SVN parent commit, and keep the list of local commits in between.
            // 488-496: Get SVN url from command line or config or fall back to commit info.
            // ...      clone commit in svn rev, rebase (make the git commit go away ?)
            // 607:     Restore $old_head.
            //
            // linearize_history:
            // 1455-1457: Take a list of commit refs, and make a hash of commit => [parents] in %parents.
            // profit?
        }
    }
}
