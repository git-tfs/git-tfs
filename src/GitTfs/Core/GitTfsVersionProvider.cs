using System.Reflection;

using GitTfs.Core.TfsInterop;

namespace GitTfs.Core
{
    public class GitTfsVersionProvider : IGitTfsVersionProvider
    {
        private readonly ITfsHelper _tfsHelper;

        public GitTfsVersionProvider(ITfsHelper tfsHelper)
        {
            _tfsHelper = tfsHelper;
        }

        public string GetVersionString() => $"git-tfs version {GetType().Assembly.GetName().Version} (TFS client library {_tfsHelper.TfsClientLibraryVersion}) ({(Environment.Is64BitProcess ? "64" : "32")}-bit)";

        public string GetPathToGitTfsExecutable() => Assembly.GetExecutingAssembly().Location;
    }
}