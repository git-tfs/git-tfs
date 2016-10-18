using System;
using System.Reflection;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
{
    public class GitTfsVersionProvider : IGitTfsVersionProvider
    {
        private ITfsHelper tfsHelper;

        public GitTfsVersionProvider(ITfsHelper tfsHelper)
        {
            this.tfsHelper = tfsHelper;
        }

        public string GetVersionString()
        {
            return string.Format("git-tfs version {0} (TFS client library {1}) ({2}-bit)",
                       GetType().Assembly.GetName().Version,
                       tfsHelper.TfsClientLibraryVersion,
                       (Environment.Is64BitProcess ? "64" : "32"));
        }

        public string GetPathToGitTfsExecutable()
        {
            return Assembly.GetExecutingAssembly().Location;
        }
    }
}