using System.IO;
using Sep.Git.Tfs.VsCommon;
using StructureMap;

namespace Sep.Git.Tfs.Vs2012
{
    public class TfsHelper : TfsHelperVs2012Base
    {
        protected override string TfsVersionString { get { return "11.0"; } }

        public TfsHelper(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        { }
    }
}
