using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs
{
    [StructureMapSingleton]
    public class Globals
    {
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
                    { "I|auto-tfs-remote|auto-remote", "Autodetect (from git history) the remote ID of the TFS to interact with",
                        v => AutoFindRemote = v != null },
                    { "A|authors=", "Path to an Authors file to map TFS users to Git users (will be kept in cache and used for all the following commands)",
                        v => AuthorsFilePath = v },
                };
            }
        }

        public string AuthorsFilePath { get; set; }
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

        public string UserSpecifiedRemoteId
        {
            get { return _userSpecifiedRemoteId; }
            set { RemoteId = _userSpecifiedRemoteId = value; }
        }
        private string _userSpecifiedRemoteId;

        public bool AutoFindRemote { get; set; }

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

        private string _gitVersion;
        public string GitVersion
        {
            get
            {
                if (_gitVersion != null)
                    return _gitVersion;
                if (Repository == null)
                    return null;
                return _gitVersion = Repository.CommandOneline("--version");
            }
        }

        public void WarnOnGitVersion(TextWriter stdout)
        {
            if (GitVersion != null && GitVersion.Contains("git version 1.8.4"))
                stdout.WriteLine(@"WARNING!!!! You are using a version of git (1.8.4) that causes problems when using git-tfs!
If you are experiencing some crashes using git-tfs, perhaps you could get a newer or older version of git.
For more information, see https://github.com/git-tfs/git-tfs/issues/448 ");
        }

        public int GcPeriod
        {
            get { return 200; }
        }
    }
}
