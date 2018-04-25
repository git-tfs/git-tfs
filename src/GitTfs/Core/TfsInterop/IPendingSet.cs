using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitTfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core.TfsInterop 
{
    public interface IPendingSet 
    {

        string Computer { get; }
        string Name { get; }
        string OwnerDisplayName { get; }
        string OwnerName { get; }

        IPendingChange[] PendingChanges { get; }
    }
}
