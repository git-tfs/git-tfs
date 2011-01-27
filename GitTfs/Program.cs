using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.Changes.Git;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;
using StructureMap.Graph;

namespace Sep.Git.Tfs
{
    public class Program
    {
        public static void Main(string [] args)
        {
            try
            {
                //Trace.Listeners.Add(new ConsoleTraceListener());
                var container = Initialize();
                container.GetInstance<GitTfs>().Run(new List<string>(args));
            }
            catch(GitTfsException e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e.Message);
                if (!e.RecommendedSolutions.IsEmpty())
                {
                    Console.WriteLine("You may be able to resolve this problem.");
                    foreach (var solution in e.RecommendedSolutions)
                    {
                        Console.WriteLine("- " + solution);
                    }
                }
                Environment.ExitCode = -1;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);
                Environment.ExitCode = -1;
            }
        }

        private static IContainer Initialize()
        {
            return new Container(Initialize);
        }

        private static void Initialize(ConfigurationExpression initializer)
        {
            var tfsPlugin = TfsPlugin.Find();
            initializer.Scan(x => { Initialize(x); tfsPlugin.Initialize(x); });
            initializer.For<TextWriter>().Use(() => Console.Out);
            initializer.For<IGitRepository>().Add<GitRepository>();
            AddGitChangeTypes(initializer);
            DoCustomConfiguration(initializer);
            tfsPlugin.Initialize(initializer);
        }

        public static void AddGitChangeTypes(ConfigurationExpression initializer)
        {
            initializer.For<IGitChangedFile>().Use<Add>().Named("A");
            initializer.For<IGitChangedFile>().Use<Modify>().Named("M");
            initializer.For<IGitChangedFile>().Use<Delete>().Named("D");
            initializer.For<IGitChangedFile>().Use<RenameEdit>().Named("R");
        }

        private static void Initialize(IAssemblyScanner scan)
        {
            scan.WithDefaultConventions();
            scan.TheCallingAssembly();
        }

        private static void DoCustomConfiguration(ConfigurationExpression initializer)
        {
            foreach(var type in typeof(Program).Assembly.GetTypes())
            {
                foreach(ConfiguresStructureMap attribute in type.GetCustomAttributes(typeof(ConfiguresStructureMap), false))
                {
                    attribute.Initialize(initializer, type);
                }
            }
        }
    }
}
