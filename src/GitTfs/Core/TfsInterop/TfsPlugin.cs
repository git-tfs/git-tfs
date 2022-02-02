using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using StructureMap;
using StructureMap.Graph;
using System.IO;
#if NETFRAMEWORK
using Microsoft.Win32;
#endif

namespace GitTfs.Core.TfsInterop
{
    public abstract class TfsPlugin
    {
        public static TfsPlugin Find()
        {
            var pluginLoader = new PluginLoader();
            var explicitVersion = Environment.GetEnvironmentVariable("GIT_TFS_CLIENT");
            if (!string.IsNullOrEmpty(explicitVersion))
            {
                return pluginLoader.TryLoadVsPluginVersion(explicitVersion) ??
                       pluginLoader.Fail("Unable to load TFS version specified in GIT_TFS_CLIENT (" + explicitVersion + ")!");
            }

            // The loop will return the first first entry, as in practice loading
            // the GitTFS.VSxxxx assembly only fails if it isn't build or can't be found.
            foreach (string version in SupportedVersions)
            {
                TfsPlugin plugin = pluginLoader.TryLoadVsPluginVersion(version);
                if (plugin != null)
                    return plugin;
            }

            return pluginLoader.Fail();
        }

        public static IReadOnlyList<string> SupportedVersions
        {
            get
            {
                // Filter out the Fake version, as this is only internal for testing
                // and we don't it to show up in user facing output/help messages.
                return PluginLoader.SupportedVersions.Except(new[] {"Fake"}).ToList();
            }
        }

        private class PluginLoader
        {
            private readonly List<Exception> _failures = new List<Exception>();
            private static string VsPluginAssemblyFolder { get; set; }

            /// <summary>
            /// List of supported Visual Studio versions. The order matters, as it influences
            /// the priority in which we try to load the corresponding <see cref="TfsPlugin"/>.
            /// </summary>
            public static IReadOnlyList<string> SupportedVersions => new List<string>
            {
                "2022",
                "2019",
                "2017",
                "2015",
                "Fake"
            };

            public TfsPlugin TryLoadVsPluginVersion(string version)
            {
                if (!SupportedVersions.Contains(version, StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine("Visual Studio " + version + " not supported...");
                    return null;
                }
                var assembly = "GitTfs.Vs" + version;
                return Try(assembly, "GitTfs.TfsPlugin");
            }

            public TfsPlugin Try(string assembly, string pluginType)
            {
                VsPluginAssemblyFolder = assembly;
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += LoadFromSameFolder;
                try
                {
                    var plugin = (TfsPlugin)Activator.CreateInstance(Assembly.Load(assembly).GetType(pluginType));
                    if (plugin.IsViable())
                    {
                        return plugin;
                    }
                }
                catch (Exception e)
                {
                    _failures.Add(e);
                }
                currentDomain.AssemblyResolve -= LoadFromSameFolder;
                return null;
            }

            private static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
            {
                string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string assemblyPath = Path.Combine(folderPath, VsPluginAssemblyFolder, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath) == false) return null;
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }

            public TfsPlugin Fail()
            {
                throw new PluginLoaderException(_failures);
            }

            public TfsPlugin Fail(string message)
            {
                throw new PluginLoaderException(message, _failures);
            }

            private class PluginLoaderException : Exception
            {
                public IEnumerable<Exception> InnerExceptions { get; private set; }

                public PluginLoaderException(string message, IEnumerable<Exception> failures) : base(message, failures.LastOrDefault())
                {
                    InnerExceptions = failures;
                }

                public PluginLoaderException(IEnumerable<Exception> failures) : this("Unable to load any TFS assemblies!", failures)
                { }
            }
        }

        public virtual void Initialize(IAssemblyScanner scan)
        {
            scan.AssemblyContainingType(GetType());
        }

        public virtual void Initialize(ConfigurationExpression config)
        {
            // Mark the ITfsHelper as a singleton to ensure that we create it only once.
            // Otherwise, it is created e.g. for every remote which is wasteful.
            config.For<ITfsHelper>().Singleton();
        }

        public abstract bool IsViable();
    }
}
