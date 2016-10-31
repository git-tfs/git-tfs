using System;
using System.Reflection;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class GitTfsVersionProvider : IGitTfsVersionProvider
    {
        private readonly ITfsHelper _tfsHelper;

        public GitTfsVersionProvider(ITfsHelper tfsHelper)
        {
            _tfsHelper = tfsHelper;
        }

        public string GetVersionString()
        {
            return string.Format("git-tfs version {0} (TFS client library {1}) ({2}-bit)",
                       GetType().Assembly.GetName().Version,
                       _tfsHelper.TfsClientLibraryVersion,
                       (Environment.Is64BitProcess ? "64" : "32"));
        }

        public string GetPathToGitTfsExecutable()
        {
            return Assembly.GetExecutingAssembly().Location;
        }
    }
}