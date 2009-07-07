using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Test.TestHelpers;
using StructureMap.AutoMocking;

namespace Sep.Git.Tfs.Test.Commands
{
    [TestClass]
    public class HelpTest
    {
        private StringWriter outputWriter;
        private RhinoAutoMocker<Help> mocks;

        [TestInitialize]
        public void Setup()
        {
            outputWriter = new StringWriter();
            mocks = new RhinoAutoMocker<Help>(MockMode.AAA);
            mocks.Inject<TextWriter>(outputWriter);
        }

        [TestMethod, Ignore /* doesn't pass on build server because rhino mocks can't be loaded. test run config problem? */]
        public void ShouldWriteGeneralHelp()
        {
            mocks.ClassUnderTest.Run(new string[0]);

            var output = outputWriter.GetStringBuilder().ToString();
            output.AssertStartsWith("Usage: git-tfs [command]");
            output.TrimEnd().AssertEndsWith(" (use 'git-tfs help [command]' for more information)");
        }

        [TestMethod, Ignore /* mock registration doesn't work right */]
        public void ShouldWriteCommandHelp()
        {
            mocks.Container.PluginGraph.CreateFamily(typeof (GitTfsCommand));
            mocks.Container.PluginGraph.FindFamily(typeof (GitTfsCommand)).AddType(typeof (TestCommand), "test");
            //mocks.Container.Inject<GitTfsCommand>("test", new TestCommand());
            mocks.ClassUnderTest.Run(new[]{"test"});

            var output = outputWriter.GetStringBuilder().ToString();
            output = Regex.Replace(output, "\r\n?|\n", "~");
            Assert.AreEqual("abc", output);
        }

        public class TestCommand : GitTfsCommand
        {
            [OptDef(OptValType.Flag)]
            [ShortOptionName('s')]
            public bool Flag { get; set; }

            public static IEnumerable<IOptionResults> TestOptions = new List<IOptionResults>();

            public IEnumerable<IOptionResults> ExtraOptions
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
