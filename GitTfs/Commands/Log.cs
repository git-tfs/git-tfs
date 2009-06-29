namespace Sep.Git.Tfs.Commands
{
    [Pluggable("history")]
    [Pluggable("log")]
    public class Log : GitTfsCommand
    {
        [OptDef(OptValType.ValueReq)]
        public int limit { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('r')]
        public string revision { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('v')]
        public bool verbose { get; set; }

        [OptDef(OptValType.Flag)]
        public bool incremental { get; set; }

        [OptDef(OptValType.Flag)]
        public bool oneline { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("show-commit")]
        [UseNameAsLongOptionName(false)]
        public bool ShowCommit { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("non-recursive")]
        [UseNameAsLongOptionName(false)]
        public bool NonRecursive { get; set; }

        [OptDef(OptValType.ValueReq)]
        [LongOptionName("authors-file")]
        [ShortOptionName('A')]
        [UseNameAsLongOptionName(false)]
        public string AuthorsFile { get; set; }

        //public bool color { get; set; }

        [OptDef(OptValType.ValueReq)]
        public string pager { get; set; }
    }
}
