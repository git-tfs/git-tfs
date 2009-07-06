using System.Collections.Generic;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Tfs
{
    public interface ITfsHelper
    {
        string TfsClientLibraryVersion { get; }
        IEnumerable<TfsChangeset> GetChangesets(long firstChangeset);
    }
}
