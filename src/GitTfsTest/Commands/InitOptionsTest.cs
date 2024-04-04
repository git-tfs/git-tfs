using GitTfs.Commands;
using StructureMap.AutoMocking;
using NDesk.Options;
using Xunit;

namespace GitTfs.Test.Commands
{
    public class InitOptionsTest : BaseTest
    {
        private readonly MoqAutoMocker<InitOptions> mocks;

        public InitOptionsTest()
        {
            mocks = new MoqAutoMocker<InitOptions>();
        }

        #region autocrlf option tests

        [Fact]
        public void AutoCrlfDefault() => Assert.Equal("false", mocks.ClassUnderTest.GitInitAutoCrlf);

        [Fact]
        public void AutoCrlfProvideTrue()
        {
            string[] args = { "init", "--autocrlf=true", "http://example.com/tfs", "$/Junk" };
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

        [Fact]
        public void AutoCrlfProvidedNoArg()
        {
            string[] args = { "init", "--autocrlf", "http://example.com/tfs", "$/Junk" };
            Assert.Throws<OptionException>(() => mocks.ClassUnderTest.OptionSet.Parse(args));
            Assert.Equal("false", mocks.ClassUnderTest.GitInitAutoCrlf);
        }

        #endregion

        #region ignorecase option tests

        [Fact]
        public void IgnorecaseDefault() =>
            // depends on global setting..
            Assert.Null(mocks.ClassUnderTest.GitInitIgnoreCase);

        [Fact]
        public void IgnoreCaseProvideTrue()
        {
            string[] args = { "init", "--ignorecase=true", "http://example.com/tfs", "$/Junk" };
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("true", mocks.ClassUnderTest.GitInitIgnoreCase);
        }

        [Fact]
        public void IgnoreCaseProvideFalse()
        {
            string[] args = { "init", "--ignorecase=false", "http://example.com/tfs", "$/Junk" };
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("false", mocks.ClassUnderTest.GitInitIgnoreCase);
        }

        [Fact]
        public void IgnoreCaseProvideInvalidOption()
        {
            string[] args = { "init", "--ignorecase=windows", "http://example.com/tfs", "$/Junk" };
            Assert.Throws<OptionException>(() => mocks.ClassUnderTest.OptionSet.Parse(args));
            Assert.Null(mocks.ClassUnderTest.GitInitIgnoreCase);
        }

        [Fact]
        public void IgnoreCaseProvideNoArg()
        {
            string[] args = { "init", "--ignorecase", "http://example.com/tfs", "$/Junk" };
            Assert.Throws<OptionException>(() => mocks.ClassUnderTest.OptionSet.Parse(args));
            Assert.Null(mocks.ClassUnderTest.GitInitIgnoreCase);
        }

        #endregion
    }
}
