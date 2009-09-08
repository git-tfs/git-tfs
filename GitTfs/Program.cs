using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.Changes.Git;
using Sep.Git.Tfs.Util;
using StructureMap;
using StructureMap.Configuration.DSL;
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
                new GitTfs().Run(new List<string>(args));
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
            ObjectFactory.Initialize(Initialize);
        }

        private static void Initialize(IInitializationExpression initializer)
        {
            initializer.Scan(Initialize);
            initializer.ForRequestedType<TextWriter>().TheDefault.Is.ConstructedBy(() => Console.Out);
            AddGitChangeTypes(initializer);
            DoCustomConfiguration(initializer);
        }

        private static void AddGitChangeTypes(IInitializationExpression initializer)
        {
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<Add>().WithName("A");
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<Modify>().WithName("M");
            initializer.InstanceOf<IGitChangedFile>().Is.OfConcreteType<Delete>().WithName("D");
        }

        private static void Initialize(IAssemblyScanner scan)
        {
            scan.WithDefaultConventions();
            scan.TheCallingAssembly();
            scan.AssemblyContainingType(typeof(Microsoft.TeamFoundation.Client.TeamFoundationServer));
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
