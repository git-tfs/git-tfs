using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using StructureMap;
using StructureMap.Graph;
using System.IO;
using Microsoft.Win32;

namespace Sep.Git.Tfs.Core.TfsInterop
{
    public abstract class TfsPlugin
    {
        public static TfsPlugin Find()
        {
            var x = new PluginLoader();
            var explicitVersion = Environment.GetEnvironmentVariable("GIT_TFS_CLIENT");
            if (explicitVersion == "11") explicitVersion = "2012"; // GitTfs.Vs2012 was formerly called GitTfs.Vs11
            if(!String.IsNullOrEmpty(explicitVersion))
            {
                return x.Try("GitTfs.Vs" + explicitVersion, "Sep.Git.Tfs.TfsPlugin") ??
                       x.Fail("Unable to load TFS version specified in GIT_TFS_CLIENT (" + explicitVersion + ")!");
            }
            //http://blogs.msdn.com/b/heaths/archive/2015/04/13/detection-keys-for-visual-studio-2015.aspx
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DevDiv\vs\Servicing\14.0\community Install=1
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\DevDiv\vs\Servicing\14.0\community
            return (IsVisualStudioInstalled("14.0") ? x.Try("GitTfs.Vs2015", "Sep.Git.Tfs.TfsPlugin") : null) ??
                   x.Try("GitTfs.Vs2013", "Sep.Git.Tfs.TfsPlugin") ??
                   x.Try("GitTfs.Vs2012", "Sep.Git.Tfs.TfsPlugin") ??
                   x.Try("GitTfs.Vs2010", "Sep.Git.Tfs.TfsPlugin") ??
                   x.Try("GitTfs.Vs2015", "Sep.Git.Tfs.TfsPlugin") ??
                   x.Fail();
        }

        private static bool IsVisualStudioInstalled(string version)
        {
            var isInstalled = TryGetRegString(@"SOFTWARE\Wow6432Node\Microsoft\DevDiv\vs\Servicing\" + version)
                || TryGetRegString(@"SOFTWARE\Microsoft\DevDiv\vs\Servicing\" + version);
            if (!isInstalled)
            {
                Trace.WriteLine("Visual Studio v"+ version + " not found...");
            }
            else
            {
                Trace.WriteLine("Visual Studio v"+ version + " detected...");
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

        class PluginLoader
        {
            private List<Exception> _failures = new List<Exception>();
            private static string VsPluginAssemblyFolder { get; set; }

            public TfsPlugin Try(string assembly, string pluginType)
            {
                VsPluginAssemblyFolder = assembly;
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += LoadFromSameFolder;
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
                currentDomain.AssemblyResolve -= LoadFromSameFolder;
                return null;
            }

            static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
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
