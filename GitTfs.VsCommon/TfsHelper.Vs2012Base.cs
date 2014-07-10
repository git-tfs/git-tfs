using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;
using StructureMap;

namespace Sep.Git.Tfs.VsCommon
{
    public abstract class TfsHelperVs2012Base : TfsHelperBase
    {
        protected TfsHelperVs2012Base(TextWriter stdout, TfsApiBridge bridge, IContainer container) : base(stdout, bridge, container)
        {
        }

        protected override bool HasWorkItems(Changeset changeset)
        {
            return Retry.Do(() => changeset.AssociatedWorkItems.Length > 0);
        }
    }
}
