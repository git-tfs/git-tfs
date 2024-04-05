using System.Diagnostics;

namespace GitTfs.Core
{
    public class TfsWriter
    {
        private readonly Globals _globals;

        public TfsWriter(Globals globals)
        {
            _globals = globals;
        }

        public int Write(string refToWrite, Func<TfsChangesetInfo, string, int> write)
        {
            var tfsParents = _globals.Repository.GetLastParentTfsCommits(refToWrite);
            if (_globals.RemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == _globals.RemoteId);

            return WriteWith(tfsParents, refToWrite, write);
        }

        private int WriteWith(IEnumerable<TfsChangesetInfo> tfsParents, string refToWrite, Func<TfsChangesetInfo, string, int> write)
        {
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    return write(changeset, refToWrite);
                case 0:
                    Trace.TraceError("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    if (tfsParents.Select(x => x.Remote.IsSubtree ? x.Remote.OwningRemoteId : x.Remote.Id).Distinct().Count() == 1)
                    {
                        //this occurs when we have merged in a subtree, at the subtree merge commit there will be a tfsParent for the
                        //subtree owner in addition to the one for the subtree itself.  In this case, we will use the subtree owner, since
                        //we are in the main history line and not a subtree line.
                        var lastChangeSet = tfsParents.OrderByDescending(x => x.ChangesetId).First();
                        if (lastChangeSet.Remote.IsSubtree)
                            lastChangeSet.Remote = _globals.Repository.ReadTfsRemote(lastChangeSet.Remote.OwningRemoteId);
                        Trace.TraceInformation("Basing from parent '{0}:{1}', use -i to override", lastChangeSet.Remote.Id, lastChangeSet.ChangesetId);
                        return write(lastChangeSet, refToWrite);
                    }

                    Trace.TraceWarning("More than one parent found! Use -i to choose the correct parent from: ");
                    foreach (var parent in tfsParents)
                    {
                        Trace.TraceWarning("  " + parent.Remote.Id);
                    }
                    return GitTfsExitCodes.InvalidArguments;
            }
        }
    }
}
