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

            public override MethodResult Execute(object testClass)
            {
                if(IsUnix())
                    return new SkipResult(testMethod, DisplayName, "This test does not work on unix-like OSes yet.");

                return base.Execute(testClass);
            }

            private bool IsUnix()
            {
                return Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;
            }
        }
    }
}
