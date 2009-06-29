namespace Sep.Git.Tfs.Commands
{
    [Pluggable("rebase")]
    public class Rebase : GitTfsCommand
    {
        [OptDef(OptValType.Flag)]
        [ShortOptionName('m')]
        [ShortOptionName('M')]
        public bool merge { get; set }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('v')]
        public bool verbose { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('s')]
        public string strategy { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('l')]
        public bool local { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("fetch-all")]
        public bool all { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('n')]
        [LongOptionName("dry-run")]
        [UseNameAsLongOptionName(false)]
        public bool DryRun { get; set; }

        private FcOptions fcOptions;
        private RemoteOptions remoteOptions;
    }
}
