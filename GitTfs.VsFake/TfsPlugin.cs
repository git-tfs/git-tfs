namespace Sep.Git.Tfs.VsFake
{
    class TfsPlugin : Sep.Git.Tfs.Core.TfsInterop.TfsPlugin
    {
        /*
        public override void Initialize(StructureMap.Graph.IAssemblyScanner scan)
        {
            base.Initialize(scan);
        }

        public override void Initialize(StructureMap.ConfigurationExpression config)
        {
        }
        */

        public override bool IsViable()
        {
            return true; // lies...
        }
    }
}
