using GitTfs.VsCommon;
using StructureMap;

namespace GitTfs.Vs2013
{
    public class TfsHelper : TfsHelperVs2012Base
    {
        protected override string TfsVersionString { get { return "12.0"; } }

        public TfsHelper(TfsApiBridge bridge, IContainer container)
            : base(bridge, container)
        { }
    }
}
