using System;
using System.ComponentModel;
using System.Diagnostics;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs
{
    [StructureMapSingleton]
    public class Globals
    {
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

        [OptDef(OptValType.Flag)]
        [ShortOptionName('d')]
        [LongOptionName("debug")]
        [UseNameAsLongOption(false)]
        [Description("Show lots of output.")]
        public bool DebugOutput
        {
            get { return _debugTraceListener.HasValue; }
            set
            {
                if (value)
                {
                    if (_debugTraceListener == null)
                    {
                        _debugTraceListener = Trace.Listeners.Add(new ConsoleTraceListener());
                    }
                }
                else
                {
                    if (_debugTraceListener != null)
                    {
                        Trace.Listeners.RemoveAt(_debugTraceListener.Value);
                    }
                }
            }
        }
        private int? _debugTraceListener;

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('i')]
        [LongOptionName("tfs-remote")]
        [LongOptionName("remote")]
        [LongOptionName("id")]
        [UseNameAsLongOption(false)]
        [Description("An optional remote ID, useful if this repository will track multiple TFS repositories.")]
        public string UserSpecifiedRemoteId
        {
            get { return _userSpecifiedRemoteId; }
            set { RemoteId = _userSpecifiedRemoteId = value; }
        }
        private string _userSpecifiedRemoteId;

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

        public string GitDir
        {
            get { return Environment.GetEnvironmentVariable("GIT_DIR"); }
            set { Environment.SetEnvironmentVariable("GIT_DIR", value); }
        }

        public bool GitDirSetByUser { get; set; }

        public string StartingRepositorySubDir { get; set; }

        public IGitRepository Repository { get; set; }

        public int GcCountdown { get; set; }

        public int GcPeriod
        {
            get { return 200; }
        }
    }
}
