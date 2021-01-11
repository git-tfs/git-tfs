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
            return pluginLoader.TryLoadVsPluginVersion("2017", true) ??
                   pluginLoader.TryLoadVsPluginVersion("2017") ??
                   pluginLoader.TryLoadVsPluginVersion("2015") ??
                   pluginLoader.Fail();
        }

        public static IReadOnlyList<string> SupportedVersion => PluginLoader.SupportedVersion;

        private class PluginLoader
        {
            private readonly List<Exception> _failures = new List<Exception>();
            private static string VsPluginAssemblyFolder { get; set; }
            public static IReadOnlyList<string> SupportedVersion => VisualStudioVersions.Keys.OrderBy(k => k).ToList();
            private static readonly Dictionary<string, string> VisualStudioVersions = new Dictionary<string, string>()
            {
                {"2017", "15.0" },
                {"2015", "14.0" },
            };

            public TfsPlugin TryLoadVsPluginVersion(string version, bool isVisualStudioRequired = false)
            {
                var assembly = "GitTfs.Vs" + version;
#if NETFRAMEWORK
                if (isVisualStudioRequired && !IsVisualStudioInstalled(version))
                {
                    return null;
                }
#endif

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

#if NETFRAMEWORK
            private static bool IsVisualStudioInstalled(string version)
            {
                if (!VisualStudioVersions.ContainsKey(version))
                {
                    Trace.WriteLine("Visual Studio " + version + " not supported...");
                    return false;
                }

                var versionCode = VisualStudioVersions[version];
                //doc: http://blogs.msdn.com/b/heaths/archive/2015/04/13/detection-keys-for-visual-studio-2015.aspx
                var isInstalled = TryGetRegString(@"SOFTWARE\Wow6432Node\Microsoft\DevDiv\vs\Servicing\" + versionCode)
                    || TryGetRegString(@"SOFTWARE\Microsoft\DevDiv\vs\Servicing\" + versionCode);

                if (!isInstalled)
                {
                    Trace.WriteLine("Visual Studio " + version + " not found...");
                }
                else
                {
                    Trace.WriteLine("Visual Studio " + version + " detected...");
                }
                return isInstalled;
            }

            private static bool TryGetRegString(string path)
            {
                RegistryKey registryKey = Registry.LocalMachine;
                try
                {
                    Trace.WriteLine("Trying to get " + registryKey.Name + "\\" + path);
                    var key = registryKey.OpenSubKey(path);
                    if (key != null)
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Unable to get registry value " + registryKey.Name + "\\" + path + ": " + e);
                }
                return false;
            }
#endif

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
