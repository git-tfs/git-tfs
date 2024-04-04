
namespace GitTfs
{
    internal class TfsPlugin : Core.TfsInterop.TfsPlugin
    {
        public override void Initialize(StructureMap.Graph.IAssemblyScanner scan)
        {
            base.Initialize(scan);
            scan.AssemblyContainingType(typeof(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection));
        }

        public override void Initialize(StructureMap.ConfigurationExpression config) => base.Initialize(config);

        public override bool IsViable() => null != typeof(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection).Assembly;
    }
}