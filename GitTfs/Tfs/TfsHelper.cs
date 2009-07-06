using System.Collections.Generic;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Tfs
{
    public class TfsHelper : ITfsHelper
    {
        public string TfsClientLibraryVersion
        {
            get { throw new System.NotImplementedException(); }
        }

        public IEnumerable<TfsChangeset> GetChangesets(long firstChangeset)
        {
            throw new System.NotImplementedException();
        }
    }
}