using System;
using CommandLine.OptParse;

namespace Sep.Git.Tfs
{
    public class Globals
    {
        public string GitDir
        {
            get { return Environment.GetEnvironmentVariable("GIT_DIR"); }
            set { Environment.SetEnvironmentVariable("GIT_DIR", value); }
        }

        public bool GitDirSetByUser { get; set; }
        public string StartingRepositorySubDir { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('R')]
        [LongOptionName("svn-remote")]
        [LongOptionName("remote")]
        [UseNameAsLongOption(false)]
        public string RepositoryIdOption
        {
            get { return RepositoryId; }
            set
            {
                NoReuseExisting = true;
                RepositoryId = value;
            }
        }

        public bool NoReuseExisting { get; set; }

        public string RepositoryId { get; set; }

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('i')]
        [LongOptionName("id")]
        [UseNameAsLongOption(false)]
        [Description("An optional remote ID, useful if this repository will track multiple TFS repositories.")]
        public string RefId { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('h')]
        [ShortOptionName('H')]
        [LongOptionName("help")]
        [UseNameAsLongOption(false)]
        public bool ShowHelp { get; set; }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('V')]
        [LongOptionName("version")]
        [UseNameAsLongOption(false)]
        public bool ShowVersion { get; set; }

        public IGit Repository { get; set; }
    }
}
