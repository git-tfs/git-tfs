using System.Diagnostics;
using System.Reflection;
using GitTfs.Core;
using GitTfs.Core.Changes.Git;
using GitTfs.Core.TfsInterop;
using GitTfs.Util;
using StructureMap;
using StructureMap.Graph;
using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Targets;

namespace GitTfs
{
    public static class Program
    {
        private static string _logFilePath;

        [STAThread]
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
            if (gitTfsException != null)
            {
                Trace.WriteLine(gitTfsException);
                Trace.TraceError(gitTfsException.Message);
                if (gitTfsException.InnerException != null)
                    ReportException(gitTfsException.InnerException);
                if (!gitTfsException.RecommendedSolutions.IsNullOrEmpty())
                {
                    Trace.TraceError("You may be able to resolve this problem.");
                    foreach (var solution in gitTfsException.RecommendedSolutions)
                    {
                        Trace.TraceError("- " + solution);
                    }
                }
            }
            else
            {
                ReportInternalException(e);
            }

            Trace.TraceWarning("All the logs could be found in the log file: " + _logFilePath);
        }

        private static void ReportInternalException(Exception e)
        {
            Trace.WriteLine(e);
            while (e is TargetInvocationException && e.InnerException != null)
                e = e.InnerException;
            while (e != null)
            {
                var gitCommandException = e as GitCommandException;
                if (gitCommandException != null)
                    Trace.TraceError("error running command: " + gitCommandException.Process.StartInfo.FileName + " " + gitCommandException.Process.StartInfo.Arguments);

                Trace.TraceError(e.Message);
                e = e.InnerException;
            }
        }

        private static IContainer Initialize() => new Container(Initialize);

        private static void Initialize(ConfigurationExpression initializer)
        {
            ConfigureLogger();
            var tfsPlugin = TfsPlugin.Find();
            initializer.Scan(x => { Initialize(x); tfsPlugin.Initialize(x); });
            initializer.For<IGitRepository>().Add<GitRepository>();
            AddGitChangeTypes(initializer);
            DoCustomConfiguration(initializer);
            tfsPlugin.Initialize(initializer);
        }

        private static void ConfigureLogger()
        {
            try
            {
                //Step 1.Create configuration object
                var config = new LoggingConfiguration();

                // Step 2. Create targets and add them to the configuration
                var consoleTarget = new ColoredConsoleTarget();
                config.AddTarget("console", consoleTarget);

                var fileTarget = new FileTarget();
                config.AddTarget("file", fileTarget);

                if (Console.BackgroundColor == ConsoleColor.White)
                {
                    ChangeConsoleColorForLevel(consoleTarget, nameof(LogLevel.Info), ConsoleOutputColor.Black);
                    ChangeConsoleColorForLevel(consoleTarget, nameof(LogLevel.Error), ConsoleOutputColor.Red);
                }

                // Step 3. Set target properties
                consoleTarget.Layout = @"${message}";
                fileTarget.FileName = @"${specialfolder:LocalApplicationData}\git-tfs\" + GitTfsConstants.LogFileName;
                fileTarget.Layout = "${longdate} [${level}] ${message}";

                // Step 4. Define rules
                var consoleRule = new LoggingRule("*", LogLevel.Info, consoleTarget);
                config.LoggingRules.Add(consoleRule);

                var fileRule = new LoggingRule("*", LogLevel.Debug, fileTarget);
                config.LoggingRules.Add(fileRule);

                // Step 5. Activate the configuration
                LogManager.Configuration = config;

                var logger = LogManager.GetLogger("git-tfs");

                Trace.Listeners.Add(new NLogTraceListener());

                var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
                _logFilePath = fileTarget.FileName.Render(logEventInfo);
            }
            catch (Exception ex)
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
                Trace.TraceWarning("Fail to enable logging in file due to error:" + ex.Message);
            }
        }

        private static void ChangeConsoleColorForLevel(ColoredConsoleTarget consoleTarget, string level, ConsoleOutputColor foregroundColor) => consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = ConditionParser.ParseExpression("level == LogLevel." + level),
            ForegroundColor = foregroundColor
        });

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
            foreach (var type in typeof(Program).Assembly.GetTypes())
            {
                foreach (ConfiguresStructureMap attribute in type.GetCustomAttributes(typeof(ConfiguresStructureMap), false))
                {
                    attribute.Initialize(initializer, type);
                }
            }
        }
    }
}
