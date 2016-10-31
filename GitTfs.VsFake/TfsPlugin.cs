using System;
using System.IO;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.VsFake;

namespace Sep.Git.Tfs
{
    internal class TfsPlugin : Core.TfsInterop.TfsPlugin
    {
        /*
        public override void Initialize(StructureMap.Graph.IAssemblyScanner scan)
        {
            base.Initialize(scan);
        }
        */

        public override void Initialize(StructureMap.ConfigurationExpression config)
        {
            config.For<Script>().Use(() => Script.Load(ScriptPath));
        }

        public override bool IsViable()
        {
            return ScriptPath.Try(File.Exists);
        }

        internal static string ScriptPath
        {
            get
            {
                return Environment.GetEnvironmentVariable(Script.EnvVar);
            }
        }
    }
}
