using System.Collections;
using System.Collections.Generic;

namespace Sep.Git.Tfs.Core
{
    public interface ITfsHelper
    {
        string TfsClientLibraryVersion { get; }
        string Url { get; set; }
        string Username { get; set; }
        IEnumerable<ITfsChangeset> GetChangesets(string path, long startVersion);
        ITfsWorkspace CreateWorkspace(string directory, IGitTfsRemote remote, TfsChangesetInfo versionToFetch);
    }
}