using System;
using System.Reflection;
using StructureMap;
using StructureMap.Graph;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public abstract class TfsPlugin
    {
        public static TfsPlugin Find()
        {
            return (TfsPlugin) Activator.CreateInstance(Assembly.Load("GitTfs.Vs2008").GetType("Sep.Git.Tfs.Vs2008.TfsPlugin"));
        }

        public virtual void Initialize(IAssemblyScanner scan)
        {
            scan.AssemblyContainingType(GetType());
        }

        public abstract void Initialize(IInitializationExpression config);
    }
}
