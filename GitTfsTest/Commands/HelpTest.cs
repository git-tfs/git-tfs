using System;
using System.Collections.Generic;
using System.IO;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Test.TestHelpers;
using StructureMap.AutoMocking;
using NDesk.Options;
using Xunit;

namespace Sep.Git.Tfs.Test.Commands
{
    public class HelpTest
    {
        private StringWriter outputWriter;
        private RhinoAutoMocker<Help> mocks;

        public HelpTest()
        {
            outputWriter = new StringWriter();
            mocks = new RhinoAutoMocker<Help>(MockMode.AAA);
            mocks.Inject<TextWriter>(outputWriter);
        }

        [Fact]
        public void ShouldWriteGeneralHelp()
        {
            mocks.Container.PluginGraph.CreateFamily(typeof(GitTfsCommand));
            mocks.Container.PluginGraph.FindFamily(typeof(GitTfsCommand)).AddType(typeof(TestCommand), "test");
            mocks.Container.Inject<GitTfsCommand>("test", new TestCommand());
            mocks.ClassUnderTest.Run();

            var output = outputWriter.GetStringBuilder().ToString();
            output.AssertStartsWith("Usage: git-tfs [command] [options]");
            output.TrimEnd()
                  .AssertEndsWith(" (use 'git-tfs help [command]' for more information)" + Environment.NewLine +
                                  "\nFind more help in our online help : https://github.com/git-tfs/git-tfs");
        }

        [Fact]
        public void ShouldWriteCommandHelp()
        {
            mocks.Container.PluginGraph.CreateFamily(typeof (GitTfsCommand));
            mocks.Container.PluginGraph.FindFamily(typeof (GitTfsCommand)).AddType(typeof (TestCommand), "test");
            mocks.Container.Inject<GitTfsCommand>("test", new TestCommand());
            mocks.ClassUnderTest.Run(new[]{"test"});

            var output = outputWriter.GetStringBuilder().ToString();
            output.AssertStartsWith("Usage: git-tfs test [options]");
        }

        public class TestCommand : GitTfsCommand
        {
            public bool Flag { get; set; }

            OptionSet TestOptions = new OptionSet();

            public OptionSet OptionSet
            {
                get { return TestOptions; }
            }

            public int Run(IList<string> args)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
