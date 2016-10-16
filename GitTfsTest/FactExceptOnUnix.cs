using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Sep.Git.Tfs.Test
{
    public class FactExceptOnUnixAttribute : FactAttribute
    {
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            yield return new FactExceptOnUnixTestCommand(method);
        }

        class FactExceptOnUnixTestCommand : FactCommand
        {
            public FactExceptOnUnixTestCommand(IMethodInfo method) : base(method) { }
        }
    }
}
