using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StructureMap;
using StructureMap.Graph;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public abstract class TfsPlugin
    {
        public static TfsPlugin Find()
        {
            var x = new PluginLoader();
            return x.Try("GitTfs.Vs2010", "Sep.Git.Tfs.Vs2010.TfsPlugin") ??
                   x.Try("GitTfs.Vs2008", "Sep.Git.Tfs.Vs2008.TfsPlugin") ??
                   x.Fail();
        }

        class PluginLoader
        {
            private List<Exception> _failures = new List<Exception>();

            public TfsPlugin Try(string assembly, string pluginType)
            {
                try
                {
                    var plugin = (TfsPlugin)Activator.CreateInstance(Assembly.Load(assembly).GetType(pluginType));
                    if(plugin.IsViable())
                    {
                        return plugin;
                    }
                }
                catch (Exception e)
                {
                    _failures.Add(e);
                }
                return null;
            }

            public TfsPlugin Fail()
            {
                throw new PluginLoaderException(_failures);
            }

            class PluginLoaderException : Exception
            {
                public IEnumerable<Exception> InnerExceptions { get; private set; }

                public PluginLoaderException(IEnumerable<Exception> failures) : base("Unable to load any TFS assemblies!", failures.Last())
                {
                    InnerExceptions = failures;
                }
            }
        }

        public virtual void Initialize(IAssemblyScanner scan)
        {
            scan.AssemblyContainingType(GetType());
        }

        public abstract void Initialize(IInitializationExpression config);

        public abstract bool IsViable();
    }
}
