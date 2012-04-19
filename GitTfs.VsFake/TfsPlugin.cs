using System;
using System.IO;
using Sep.Git.Tfs.Core;
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
            return ScriptPath.AndAnd(File.Exists);
        }

        internal static string ScriptPath
        {
            get
            {
                return Environment.GetEnvironmentVariable(Script.EnvVar);
            }
        }

        static Script _script;
        internal static Script Script
        {
            get
            {
                if (_script == null)
                {
                    _script = ScriptPath.AndAnd(Script.Load);
                }
                return _script;
            }
        }
    }
}
