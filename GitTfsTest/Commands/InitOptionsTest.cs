using System.IO;
using Sep.Git.Tfs.Commands;
using StructureMap.AutoMocking;
using NDesk.Options;
using Xunit;

namespace Sep.Git.Tfs.Test.Commands
{
    public class InitOptionsTest
    {
        private RhinoAutoMocker<InitOptions> mocks;

        public InitOptionsTest()
        {
            mocks = new RhinoAutoMocker<InitOptions>();
        }

        [Fact]
        public void AutoCrlfDefault()
        {
            Assert.Equal("false", mocks.ClassUnderTest.GitInitAutoCrlf);
        }

        [Fact]
        public void AutoCrlfProvideTrue()
        {
            string[] args = {"init", "--autocrlf=true", "http://example.com/tfs", "$/Junk"};
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("true", mocks.ClassUnderTest.GitInitAutoCrlf);
        }

        [Fact]
        public void AutoCrlfProvideFalse()
        {
            string[] args = { "init", "--autocrlf=false", "http://example.com/tfs", "$/Junk" };
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("false", mocks.ClassUnderTest.GitInitAutoCrlf);
        }

        [Fact]
        public void AutoCrlfProvideAuto()
        {
            string[] args = { "init", "--autocrlf=auto", "http://example.com/tfs", "$/Junk" };
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("auto", mocks.ClassUnderTest.GitInitAutoCrlf);
        }

        [Fact]
        public void AutoCrlfProvideInvalidOption()
        {
            string[] args = { "init", "--autocrlf=windows", "http://example.com/tfs", "$/Junk" };
            Assert.Throws<OptionException>(() => mocks.ClassUnderTest.OptionSet.Parse(args));
            Assert.Equal("false", mocks.ClassUnderTest.GitInitAutoCrlf);
        }

    }
}
