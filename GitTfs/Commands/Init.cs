using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("init")]
    public class Init : GitTfsCommand
    {
        [OptDef(OptValType.ValueReq)]
        [Description("The --template option to pass to git-init.")]
        public string template { get; set; }

        [OptDef(OptValType.ValueReq)]
        [Description("The --shared option to pass to git-init.")]
        public string shared { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('u')]
        [Description("The URL or alias of the TFS server to use.")]
        public string tfs { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('r')]
        [LongOptionName("repo-path")]
        [UseNameAsLongOption(false)]
        [Description("The repository path in TFS that this git repository will be a mirror of.")]
        public string RepositoryPath { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-metadata")]
        [UseNameAsLongOption(false)]
        [Description("If specified, git-tfs will leave out the git-tfs-id: lines at the end of every commit.")]
        public bool NoMetaData { get; set; }

        [OptDef(OptValType.ValueReq)]
        [Description("Your TFS username, including domain.")]
        public string username { get; set; }

        public int Run(IEnumerable<string> args)
        {
            throw new System.NotImplementedException();
        }
    }
}
