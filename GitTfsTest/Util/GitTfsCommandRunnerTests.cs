using System;
using System.Collections.Generic;
using System.IO;
using CommandLine.OptParse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;

namespace Sep.Git.Tfs.Test.Util
{
    [TestClass]
    public class GitTfsCommandRunnerTests
    {
        #region Base implementation of GitTfsCommand, for tests

        public class TestCommandBase : GitTfsCommand
        {
            internal enum Form { List, Split }
            internal class Invocation
            {
                public Form Form { get; private set; }
                public IList<string> Args { get; private set; }
                private Invocation(){}
                public static Invocation List(IList<string> args)
                {
                    return new Invocation {Form = Form.List, Args = args};
                }
                public static Invocation Split(params string[]args)
                {
                    return new Invocation {Form = Form.Split, Args = args};
                }
            }
            internal List<Invocation> Calls = new List<Invocation>();
            // Implement the GitTfsCommand interface
            public IEnumerable<IOptionResults> ExtraOptions
            {
                get { return new IOptionResults[0]; }
            }
        }
        #endregion

        private RhinoAutoMocker<GitTfsCommandRunner> _mocks;

        [TestInitialize]
        public void Setup()
        {
            _mocks = new RhinoAutoMocker<GitTfsCommandRunner>(MockMode.AAA);
            _mocks.Inject<TextWriter>(new StringWriter());
        }

        IList<string> Args(params string[] args)
        {
            return args;
        }

        public class UsesList : TestCommandBase
        {
            public int Run(IList<string> args)
            {
                Calls.Add(Invocation.List(args));
                return 99;
            }
        }

        [TestMethod]
        public void ReturnsCommandReturnValue()
        {
            Assert.AreEqual(99, _mocks.ClassUnderTest.Run(new UsesList(), Args()));
        }

        [TestMethod]
        public void CallsListWithZeroArgs()
        {
            var command = new UsesList();
            var args = Args();
            _mocks.ClassUnderTest.Run(command, args);
            Assert.AreEqual(1, command.Calls.Count, "number of calls to the command");
            Assert.AreEqual(TestCommandBase.Form.List, command.Calls[0].Form, "form of the call");
            Assert.AreSame(args, command.Calls[0].Args, "arguments to the call");
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

        [TestMethod]
        public void CallsOverloadWithOneArg()
        {
            var command = new UsesOverloads();
            _mocks.ClassUnderTest.Run(command, Args("a"));
            Assert.AreEqual(1, command.Calls.Count, "number of calls to the command");
            Assert.AreEqual(TestCommandBase.Form.Split, command.Calls[0].Form, "form of the call");
            Assert.AreEqual(1, command.Calls[0].Args.Count, "number of arguments to the call");
            Assert.AreEqual("a", command.Calls[0].Args[0], "arg[0] to the call");
        }

        [TestMethod]
        public void CallsOverloadWithTwoArgs()
        {
            var command = new UsesOverloads();
            _mocks.ClassUnderTest.Run(command, Args("a", "b"));
            Assert.AreEqual(1, command.Calls.Count, "number of calls to the command");
            Assert.AreEqual(TestCommandBase.Form.Split, command.Calls[0].Form, "form of the call");
            Assert.AreEqual(2, command.Calls[0].Args.Count, "number of arguments to the call");
            Assert.AreEqual("a", command.Calls[0].Args[0], "arg[0] to the call");
            Assert.AreEqual("b", command.Calls[0].Args[1], "arg[1] to the call");
        }

        [TestMethod]
        public void ReturnsHelpForTooFewArgs()
        {
            _mocks.Get<IHelpHelper>().Stub(x => x.ShowHelpForInvalidArguments(null)).IgnoreArguments().Return(33);
            Assert.AreEqual(33, _mocks.ClassUnderTest.Run(new UsesOverloads(), Args()));
        }

        [TestMethod]
        public void ReturnsHelpForTooManyArgs()
        {
            _mocks.Get<IHelpHelper>().Stub(x => x.ShowHelpForInvalidArguments(null)).IgnoreArguments().Return(33);
            Assert.AreEqual(33, _mocks.ClassUnderTest.Run(new UsesOverloads(), Args("a", "b", "c")));
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

        [TestMethod]
        public void CallsOverloadOrDefaultWithZeroArgs()
        {
            var command = new UsesOverloadsOrDefault();
            _mocks.ClassUnderTest.Run(command, Args());
            Assert.AreEqual(1, command.Calls.Count, "number of calls to the command");
            Assert.AreEqual(TestCommandBase.Form.Split, command.Calls[0].Form, "form of the call");
            Assert.AreEqual(0, command.Calls[0].Args.Count, "number of arguments to the call");
        }

        [TestMethod]
        public void CallsOverloadOrDefaultWithOneArg()
        {
            var command = new UsesOverloadsOrDefault();
            _mocks.ClassUnderTest.Run(command, Args("a"));
            Assert.AreEqual(1, command.Calls.Count, "number of calls to the command");
            Assert.AreEqual(TestCommandBase.Form.Split, command.Calls[0].Form, "form of the call");
            Assert.AreEqual(1, command.Calls[0].Args.Count, "number of arguments to the call");
            Assert.AreEqual("a", command.Calls[0].Args[0], "arg[0] to the call");
        }

        [TestMethod]
        public void CallsOverloadOrDefaultWithTwoArgs()
        {
            var command = new UsesOverloadsOrDefault();
            var args = Args("a", "b");
            _mocks.ClassUnderTest.Run(command, args);
            Assert.AreEqual(1, command.Calls.Count, "number of calls to the command");
            Assert.AreEqual(TestCommandBase.Form.List, command.Calls[0].Form, "form of the call");
            Assert.AreSame(args, command.Calls[0].Args, "arguments to the call");
        }
    }
}
