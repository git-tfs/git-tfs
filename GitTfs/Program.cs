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
                Initialize();
                ObjectFactory.GetInstance<GitTfs>().Run(new List<string>(args));
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);
                Environment.ExitCode = -1;
            }
        }

        private static void Initialize()
        {
			Console.Read();
            ObjectFactory.Initialize(Initialize);
        }

        private static void Initialize(IInitializationExpression initializer)
        {
            var tfsPlugin = TfsPlugin.Find();
            initializer.Scan(x => { Initialize(x); tfsPlugin.Initialize(x); });
            initializer.ForRequestedType<TextWriter>().TheDefault.Is.ConstructedBy(() => Console.Out);
            initializer.InstanceOf<IGitRepository>().Is.OfConcreteType<GitRepository>();
            AddGitChangeTypes(initializer);
            DoCustomConfiguration(initializer);
            tfsPlugin.Initialize(initializer);
        }

        private static void AddGitChangeTypes(IInitializationExpression initializer)
        {
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<Add>().WithName("A");
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<Modify>().WithName("M");
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<Delete>().WithName("D");
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<RenameEdit>().WithName("R");
        }

        private static void Initialize(IAssemblyScanner scan)
        {
            scan.WithDefaultConventions();
            scan.TheCallingAssembly();
        }

        private static void DoCustomConfiguration(IInitializationExpression initializer)
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
