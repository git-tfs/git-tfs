using GitTfs.Commands;
using StructureMap.AutoMocking;
using NDesk.Options;
using Xunit;
using NLog;
using System.Diagnostics;
using NLog.Config;
using NLog.Targets;

namespace GitTfs.Test.Commands
{
    public class HelpTest : BaseTest
    {
        private readonly MoqAutoMocker<Help> mocks;

        public HelpTest()
        {
            mocks = new MoqAutoMocker<Help>();
        }

        public MemoryTarget GetTestLogger()
        {
            var memoryTarget = new MemoryTarget() { Layout = @"${message}" };

            var config = new LoggingConfiguration();
            config.AddTarget("memory", memoryTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, memoryTarget));

            LogManager.Configuration = config;

            Trace.Listeners.Add(new NLogTraceListener());

            return memoryTarget;
        }

        [Fact]
        public void ShouldWriteGeneralHelp()
        {
            var memoryTarget = GetTestLogger();

            mocks.Container.PluginGraph.FindFamily(typeof(GitTfsCommand)).AddType(typeof(TestCommand), "test");
            mocks.Container.Inject<GitTfsCommand>("test", new TestCommand());
            mocks.ClassUnderTest.Run();

            memoryTarget.Logs[0].Equals("Usage: git-tfs [command] [options]");
            memoryTarget.Logs[1].Contains("test");
            memoryTarget.Logs[2].Equals(" (use 'git-tfs help [command]' or 'git-tfs [command] --help' for more information)");
            memoryTarget.Logs[3].Equals("Find more help in our online help : https://github.com/git-tfs/git-tfs");
        }

        [Fact]
        public void ShouldWriteCommandHelp()
        {
            var memoryTarget = GetTestLogger();
            mocks.Container.PluginGraph.CreateFamily(typeof(GitTfsCommand));
            mocks.Container.PluginGraph.FindFamily(typeof(GitTfsCommand)).AddType(typeof(TestCommand), "test");
            mocks.Container.Inject<GitTfsCommand>("test", new TestCommand());
            mocks.ClassUnderTest.Run(new[] { "test" });

            memoryTarget.Logs[0].Equals("Usage: git-tfs test [options]");
        }

        public class TestCommand : GitTfsCommand
        {
            public bool Flag { get; set; }

            private readonly OptionSet TestOptions = new OptionSet();

            public OptionSet OptionSet => TestOptions;

            public int Run(IList<string> args) => throw new System.NotImplementedException();
        }
    }
}
