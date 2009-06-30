using System.ComponentModel;
using CommandLine.OptParse;

namespace Sep.Git.Tfs.Commands
{
    // based on %cmt_opts
    [StructureMapSingleton]
    public class CommitOptions
    {
        [OptDef(OptValType.Flag)]
        [ShortOptionName('e')]
        [LongOptionName("edit")]
        [UseNameAsLongOption(false)]
        [Description("Edit the commit message before committing to TFS. Default = off, unless committing tree objects. config key: tfs.edit")]
        public bool Edit { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("rmdir")]
        [UseNameAsLongOption(false)]
        [Description("Remove directories from TFS if there are no files left behind. config key: tfs.rmdir")]
        public bool RemoveEmptyDirectories { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("find-copies-harder")]
        [UseNameAsLongOption(false)]
        [Description("Passed to git-diff-tree. config key: tfs.findcopiesharder")]
        public bool FindCopiesHarder { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('l')]
        [UseNameAsLongOption(false)]
        [Description("Passed to git-diff-tree. config key: tfs.l")]
        public bool CopyRenameLimit { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('C')]
        [LongOptionName("copy-similarity")]
        [UseNameAsLongOption(false)]
        [Description("Passed to git-diff-tree. config key: tfs.copysimilarity")]
        public object CopySimilarity { get; set; }

    }
}
