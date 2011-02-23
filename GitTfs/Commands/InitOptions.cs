using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class InitOptions
    {
        [OptDef(OptValType.ValueReq)]
        [LongOptionName("template")]
        [UseNameAsLongOption(false)]
        [Description("The --template option to pass to git-init.")]
        public string GitInitTemplate { get; set; }

        // It would be better if this were an optional value, but there's no way
        // to tell the difference between an optional value that was not provided,
        // and one that was set but with no argument.
        //[OptDef(OptValType.ValueOpt)]
        [OptDef(OptValType.ValueReq)]
        [LongOptionName("shared")]
        [UseNameAsLongOption(false)]
        [Description("The --shared option to pass to git-init.")]
        public object GitInitShared { get; set; }

    }
}
