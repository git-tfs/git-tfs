using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using StructureMap;

namespace Sep.Git.Tfs
{
    public class Program
    {
        public static void Main(string [] args)
        {
            try
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
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
            initializer.Scan(scan => { scan.WithDefaultConventions(); scan.TheCallingAssembly(); scan.AssemblyContainingType(typeof(Microsoft.TeamFoundation.Client.TeamFoundationServer)); });
            initializer.ForRequestedType<TextWriter>().TheDefault.Is.ConstructedBy(() => Console.Out);
            DoCustomConfiguration(initializer);
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
