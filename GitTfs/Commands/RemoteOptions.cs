using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class RemoteOptions
    {
        [OptDef(OptValType.ValueReq)]
        [LongOptionName("ignore-regex")]
        [UseNameAsLongOption(false)]
        [Description("If specified, git-tfs will not sync any paths that match this regular expression.")]
        public string IgnoreRegex { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-metadata")]
        [UseNameAsLongOption(false)]
        [Description("If specified, git-tfs will leave out the git-tfs-id: lines at the end of every commit.")]
        public bool NoMetaData { get; set; }

    }
}
