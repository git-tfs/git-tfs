namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkin")]
    public class Checkin : GitTfsCommand
    {
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
    }
}
