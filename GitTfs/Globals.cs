using System;
using System.ComponentModel;
using System.Diagnostics;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs
{
    [StructureMapSingleton]
    public class Globals
    {
        TraceListener _listener;

        public Globals(GitTfsDebugTraceListener listener)
        {
            _listener = listener;
        }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "h|H|help",
                        v => ShowHelp = v != null },
                    { "V|version",
                        v => ShowVersion = v != null },
                    { "d|debug", "Show debug output about everything git-tfs does",
                        v => DebugOutput = v != null },
                    { "i|tfs-remote|remote|id=", "The remote ID of the TFS to interact with\ndefault: default",
                        v => UserSpecifiedRemoteId = v },
                };
            }
        }

        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }

        public bool DebugOutput
        {
            get { return _debugTraceListener.HasValue; }
            set
            {
                if (value)
                {
                    if (_debugTraceListener == null)
                    {
                        _debugTraceListener = Trace.Listeners.Add(_listener);
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

        public string UserSpecifiedRemoteId
        {
            get { return _userSpecifiedRemoteId; }
            set { RemoteId = _userSpecifiedRemoteId = value; }
        }
        private string _userSpecifiedRemoteId;

        public string RemoteId { get; set; }

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
