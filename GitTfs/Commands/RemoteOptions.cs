using System.ComponentModel;
using CommandLine.OptParse;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class RemoteOptions
    {
        [OptDef(OptValType.ValueReq)]
        [LongOptionName("username")]
        [UseNameAsLongOption(false)]
        [Description("Your TFS username, including domain (e.g. DOMAIN\\user).")]
        public string Username { get; set; }

// don't know what this is good for
//        [OptDef(OptValType.ValueReq)]
//        [LongOptionName("config-dir")]
//        [UseNameAsLongOption(false)]
//        [Description("Your TFS username, including domain (e.g. DOMAIN\\user).")]
//        public string Username { get; set; }

// this isn't used because the TFS client won't cache creds for us, whereas SVN will.
//        [OptDef(OptValType.ValueReq)]
//        [LongOptionName("no-auth-cache")]
//        [UseNameAsLongOption(false)]
//        public bool NoAuthCache { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("ignore-regex")]
        [UseNameAsLongOption(false)]
        [Description("If specified, git-tfs will not sync any paths that match this regular expression.")]
        public string IgnoreRegex { get; set; }

    }
}
