using System;
using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs
{
    [StructureMapSingleton]
    public class Globals
    {
        public string GitDir
        {
            get { return Environment.GetEnvironmentVariable("GIT_DIR"); }
            set { Environment.SetEnvironmentVariable("GIT_DIR", value); }
        }

        public bool GitDirSetByUser { get; set; }
        public string StartingRepositorySubDir { get; set; }

        // This is a merger of the SVN "remote id" and "ref id". Is there a reason for them to be separate?
        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('i')]
        [LongOptionName("tfs-remote")]
        [LongOptionName("remote")]
        [LongOptionName("id")]
        [UseNameAsLongOption(false)]
        [Description("An optional remote ID, useful if this repository will track multiple TFS repositories.")]
        public string RemoteId { get; set; }

        public string RemoteConfigPrefix
        {
            get
            {
                if (RemoteId == null) return null;
                return "tfs-remote." + RemoteId;
            }
        }

        public string RemoteConfigKey(string parameter)
        {
            return RemoteConfigPrefix + "." + parameter;
        }

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

        public IGitRepository Repository { get; set; }

        public int GcCountdown { get; set; }

        public int GcPeriod
        {
            get { return 1000; }
        }
    }
}
