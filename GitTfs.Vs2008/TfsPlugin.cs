namespace Sep.Git.Tfs.Vs2008
{
    class TfsPlugin : Sep.Git.Tfs.Core.TfsInterop.TfsPlugin
    {
        public override void Initialize(StructureMap.Graph.IAssemblyScanner scan)
        {
            base.Initialize(scan);
            scan.AssemblyContainingType(typeof (Microsoft.TeamFoundation.Client.TeamFoundationServer));
        }

        public override void Initialize(StructureMap.IInitializationExpression config)
        {
        }
    }
}
