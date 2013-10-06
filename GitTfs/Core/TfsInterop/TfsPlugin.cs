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
            var explicitVersion = Environment.GetEnvironmentVariable("GIT_TFS_CLIENT");
            if(!String.IsNullOrEmpty(explicitVersion))
            {
                return x.Try("GitTfs.Vs" + explicitVersion, "Sep.Git.Tfs.Vs" + explicitVersion + ".TfsPlugin") ??
                       x.Fail("Unable to load TFS version specified in GIT_TFS_CLIENT (" + explicitVersion + ")!");
            }
            return x.Try("GitTfs.Vs2013", "Sep.Git.Tfs.Vs2013.TfsPlugin") ??
                   x.Try("GitTfs.Vs2012", "Sep.Git.Tfs.Vs2012.TfsPlugin") ??
                   x.Try("GitTfs.Vs2010", "Sep.Git.Tfs.Vs2010.TfsPlugin") ??
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

            public TfsPlugin Fail(string message)
            {
                throw new PluginLoaderException(message, _failures);
            }

            class PluginLoaderException : Exception
            {
                public IEnumerable<Exception> InnerExceptions { get; private set; }

                public PluginLoaderException(string message, IEnumerable<Exception> failures) : base(message, failures.LastOrDefault())
                {
                    InnerExceptions = failures;
                }

                public PluginLoaderException(IEnumerable<Exception> failures) : this("Unable to load any TFS assemblies!", failures)
                {}
            }
        }

        public virtual void Initialize(IAssemblyScanner scan)
        {
            scan.AssemblyContainingType(GetType());
        }

        public virtual void Initialize(ConfigurationExpression config)
        {
        }

        public abstract bool IsViable();
    }
}
