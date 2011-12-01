using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sep.Git.Tfs.Core
{
    public class TfsWriter
    {
        private readonly TextWriter _stdout;
        private readonly Globals _globals;

        public TfsWriter(TextWriter stdout, Globals globals)
        {
            _stdout = stdout;
            _globals = globals;
        }

        public int Write(string refToWrite, Func<TfsChangesetInfo, int> write)
        {
            var tfsParents = _globals.Repository.GetLastParentTfsCommits(refToWrite);
            if (_globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == _globals.UserSpecifiedRemoteId);
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    return write(changeset);
                case 0:
                    _stdout.WriteLine("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    _stdout.WriteLine("More than one parent found! Use -i to choose the correct parent from: ");
                    foreach (var parent in tfsParents)
                    {
                        _stdout.WriteLine("  " + parent.Remote.Id);
                    }
                    return GitTfsExitCodes.InvalidArguments;
            }
        }
    }
}
