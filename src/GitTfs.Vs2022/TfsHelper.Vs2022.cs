using GitTfs.VsCommon;

using StructureMap;

namespace GitTfs.Vs2022
{
    public class TfsHelper : TfsHelperVS2017Base
    {
        public TfsHelper(TfsApiBridge bridge, IContainer container)
            : base(bridge, container, 17)
        {
        }
    }
}