using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.Changes.Git;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using StructureMap;
using StructureMap.Graph;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Sep.Git.Tfs
{
    public class Program
    {
        private static ILogger Logger;
        [STAThreadAttribute]
        public static void Main(string[] args)
        {
            try
            {
                Environment.ExitCode = MainCore(args);
            }
            catch (Exception e)
            {
                ReportException(e);
                Environment.ExitCode = GitTfsExitCodes.ExceptionThrown;
            }
        }

        public static int MainCore(string[] args)
        {
            var container = Initialize();
            return container.GetInstance<GitTfs>().Run(new List<string>(args));
        }

        private static void ReportException(Exception e)
        {
            var gitTfsException = e as GitTfsException;
            if(gitTfsException != null)
            {
                Trace.WriteLine(gitTfsException);
                Console.WriteLine(gitTfsException.Message);
                if (gitTfsException.InnerException != null)
                    ReportException(gitTfsException.InnerException);
                if (!gitTfsException.RecommendedSolutions.IsEmpty())
                {
                    Console.WriteLine("You may be able to resolve this problem.");
                    foreach (var solution in gitTfsException.RecommendedSolutions)
                    {
                        Console.WriteLine("- " + solution);
                    }
                }
            }
            else
            {
                ReportInternalException(e);
            }
        }

        private static void ReportInternalException(Exception e)
        {
            Trace.WriteLine(e);
            while(e is TargetInvocationException && e.InnerException != null)
                e = e.InnerException;
            while (e != null)
            {
                var gitCommandException = e as GitCommandException;
                if (gitCommandException != null)
                    Console.WriteLine("error running command: " + gitCommandException.Process.StartInfo.FileName + " " + gitCommandException.Process.StartInfo.Arguments);

                Console.WriteLine(e.Message);
                e = e.InnerException;
            }
        }

        private static IContainer Initialize()
        {
            return new Container(Initialize);
        }

        private static void Initialize(ConfigurationExpression initializer)
        {
            Logger = GetLogger();
            var tfsPlugin = TfsPlugin.Find();
            initializer.Scan(x => { Initialize(x); tfsPlugin.Initialize(x); });
            initializer.For<IGitRepository>().Add<GitRepository>();
            initializer.For<ILogger>().Add(Logger);
            AddGitChangeTypes(initializer);
            DoCustomConfiguration(initializer);
            tfsPlugin.Initialize(initializer);

        }

        private static ILogger GetLogger()
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            // Step 3. Set target properties 
            consoleTarget.Layout = @"${message}";
            fileTarget.FileName = @"${specialfolder:LocalApplicationData}\git-tfs\" + GitTfsConstants.LogFileName;
            fileTarget.Layout = "${message}";

            // Step 4. Define rules
            var consoleRule = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(consoleRule);

            var fileRule = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(fileRule);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;

            // Example usage
            return LogManager.GetLogger("git-tfs");
        }

        public static void AddGitChangeTypes(ConfigurationExpression initializer)
        {
            // See git-diff-tree(1).
            initializer.For<IGitChangedFile>().Use<Add>().Named(GitChangeInfo.ChangeType.ADD);
            initializer.For<IGitChangedFile>().Use<Copy>().Named(GitChangeInfo.ChangeType.COPY);
            initializer.For<IGitChangedFile>().Use<Modify>().Named(GitChangeInfo.ChangeType.MODIFY);
            //initializer.For<IGitChangedFile>().Use<TypeChange>().Named(GitChangeInfo.GitChange.TYPECHANGE);
            initializer.For<IGitChangedFile>().Use<Delete>().Named(GitChangeInfo.ChangeType.DELETE);
            initializer.For<IGitChangedFile>().Use<RenameEdit>().Named(GitChangeInfo.ChangeType.RENAMEEDIT);
            //initializer.For<IGitChangedFile>().Use<Unmerged>().Named(GitChangeInfo.GitChange.UNMERGED);
            //initializer.For<IGitChangedFile>().Use<Unknown>().Named(GitChangeInfo.GitChange.UNKNOWN);
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
