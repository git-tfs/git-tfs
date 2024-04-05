using GitTfs.Commands;
using GitTfs.Util;
using Moq;
using StructureMap.AutoMocking;
using NDesk.Options;
using Xunit;

namespace GitTfs.Test.Util
{
    public class GitTfsCommandRunnerTests : BaseTest
    {
        #region Base implementation of GitTfsCommand, for tests

        public class TestCommandBase : GitTfsCommand
        {
            internal enum Form { List, Split }
            internal class Invocation
            {
                public Form Form { get; private set; }
                public IList<string> Args { get; private set; }
                private Invocation() { }
                public static Invocation List(IList<string> args) => new Invocation { Form = Form.List, Args = args };
                public static Invocation Split(params string[] args) => new Invocation { Form = Form.Split, Args = args };
            }
            internal List<Invocation> Calls = new List<Invocation>();

            public OptionSet OptionSet { get; set; }
        }
        #endregion

        private readonly MoqAutoMocker<GitTfsCommandRunner> _mocks;

        public GitTfsCommandRunnerTests()
        {
            _mocks = new MoqAutoMocker<GitTfsCommandRunner>();
        }

        private IList<string> Args(params string[] args) => args;

        public class UsesList : TestCommandBase
        {
            public int Run(IList<string> args)
            {
                Calls.Add(Invocation.List(args));
                return 99;
            }
        }

        [Fact]
        public void ReturnsCommandReturnValue() => Assert.Equal(99, _mocks.ClassUnderTest.Run(new UsesList(), Args()));

        [Fact]
        public void CallsListWithZeroArgs()
        {
            var command = new UsesList();
            var args = Args();
            _mocks.ClassUnderTest.Run(command, args);
            Assert.Single(command.Calls);
            Assert.Equal(TestCommandBase.Form.List, command.Calls[0].Form);
            Assert.Same(args, command.Calls[0].Args);
        }

        public class UsesOverloads : TestCommandBase
        {
            public int Run(string a)
            {
                Calls.Add(Invocation.Split(a));
                return 89;
            }
            public int Run(string a, string b)
            {
                Calls.Add(Invocation.Split(a, b));
                return 88;
            }
        }

        [Fact]
        public void CallsOverloadWithOneArg()
        {
            var command = new UsesOverloads();
            var args = Args("a");
            _mocks.ClassUnderTest.Run(command, args);
            Assert.Single(command.Calls);
            Assert.Equal(TestCommandBase.Form.Split, command.Calls[0].Form);
            Assert.Equal(args, command.Calls[0].Args);
        }

        [Fact]
        public void CallsOverloadWithTwoArgs()
        {
            var command = new UsesOverloads();
            var args = Args("a", "b");
            _mocks.ClassUnderTest.Run(command, args);
            Assert.Single(command.Calls);
            Assert.Equal(TestCommandBase.Form.Split, command.Calls[0].Form);
            Assert.Equal(args, command.Calls[0].Args);
        }

        [Fact]
        public void ReturnsHelpForTooFewArgs()
        {
            Mock.Get(_mocks.Get<IHelpHelper>()).Setup(x => x.ShowHelpForInvalidArguments(It.IsAny<GitTfsCommand>())).Returns(33);
            Assert.Equal(33, _mocks.ClassUnderTest.Run(new UsesOverloads(), Args()));
        }

        [Fact]
        public void ReturnsHelpForTooManyArgs()
        {
            Mock.Get(_mocks.Get<IHelpHelper>()).Setup(x => x.ShowHelpForInvalidArguments(It.IsAny<GitTfsCommand>())).Returns(33);
            Assert.Equal(33, _mocks.ClassUnderTest.Run(new UsesOverloads(), Args("a", "b", "c")));
        }

        public class UsesOverloadsOrDefault : TestCommandBase
        {
            public int Run()
            {
                Calls.Add(Invocation.Split());
                return 79;
            }
            public int Run(string a)
            {
                Calls.Add(Invocation.Split(a));
                return 78;
            }
            public int Run(IList<string> args)
            {
                Calls.Add(Invocation.List(args));
                return 77;
            }
        }

        [Fact]
        public void CallsOverloadOrDefaultWithZeroArgs()
        {
            var command = new UsesOverloadsOrDefault();
            var args = Args();
            _mocks.ClassUnderTest.Run(command, args);
            Assert.Single(command.Calls);
            Assert.Equal(TestCommandBase.Form.Split, command.Calls[0].Form);
            Assert.Equal(args, command.Calls[0].Args);
        }

        [Fact]
        public void CallsOverloadOrDefaultWithOneArg()
        {
            var command = new UsesOverloadsOrDefault();
            var args = Args("a");
            _mocks.ClassUnderTest.Run(command, args);
            Assert.Single(command.Calls);
            Assert.Equal(TestCommandBase.Form.Split, command.Calls[0].Form);
            Assert.Equal(args, command.Calls[0].Args);
        }

        [Fact]
        public void CallsOverloadOrDefaultWithTwoArgs()
        {
            var command = new UsesOverloadsOrDefault();
            var args = Args("a", "b");
            _mocks.ClassUnderTest.Run(command, args);
            Assert.Single(command.Calls);
            Assert.Equal(TestCommandBase.Form.List, command.Calls[0].Form);
            Assert.Same(args, command.Calls[0].Args);
        }
    }
}
