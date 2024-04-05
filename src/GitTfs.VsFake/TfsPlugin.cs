using GitTfs.Core;
using GitTfs.VsFake;

namespace GitTfs
{
    internal class TfsPlugin : Core.TfsInterop.TfsPlugin
    {
        /*
        public override void Initialize(StructureMap.Graph.IAssemblyScanner scan)
        {
            base.Initialize(scan);
        }
        */

        public override void Initialize(StructureMap.ConfigurationExpression config) => config.For<Script>().Use(() => Script.Load(ScriptPath));

        public override bool IsViable() => ScriptPath.Try(File.Exists);

        internal static string ScriptPath => Environment.GetEnvironmentVariable(Script.EnvVar);
    }
}
