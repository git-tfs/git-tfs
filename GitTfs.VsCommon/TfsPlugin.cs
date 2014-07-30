namespace Sep.Git.Tfs
{
    class TfsPlugin : Sep.Git.Tfs.Core.TfsInterop.TfsPlugin
    {
        public override void Initialize(StructureMap.Graph.IAssemblyScanner scan)
        {
            base.Initialize(scan);
            scan.AssemblyContainingType(typeof(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection));
        }

        public override void Initialize(StructureMap.ConfigurationExpression config)
        {
        }

        public override bool IsViable()
        {
            return null != typeof(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection).Assembly;
        }
    }
}