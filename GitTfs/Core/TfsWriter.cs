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

        public int Write(string refToWrite, Func<TfsChangesetInfo, string, int> write)
        {
            var tfsParents = _globals.Repository.GetLastParentTfsCommits(refToWrite);
            if (_globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == _globals.UserSpecifiedRemoteId);

            return WriteWith(tfsParents, write);
        }

        private int WriteWith(IEnumerable<TfsChangesetInfo> tfsParents, Func<TfsChangesetInfo, int> write)
        {
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    return write(changeset, refToWrite);
                case 0:
                    _stdout.WriteLine("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    if (tfsParents.Select(x => x.Remote.IsSubtree ? x.Remote.OwningRemoteId : x.Remote.Id).Distinct().Count() == 1)
                    {
                        //this occurs when we have merged in a subtree, at the subtree merge commit there will be a tfsParent for the
                        //subtree owner in addition to the one for the subtree itself.  In this case, we will use the subtree owner, since
                        //we are in the main history line and not a subtree line.
                        var cs = tfsParents.First();
                        if(cs.Remote.IsSubtree)
                            cs.Remote = _globals.Repository.ReadTfsRemote(cs.Remote.OwningRemoteId);
                        _stdout.WriteLine(string.Format("Basing from parent '{0}', use -i to override", cs.Remote.Id));
                        return write(cs);
                    }

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
