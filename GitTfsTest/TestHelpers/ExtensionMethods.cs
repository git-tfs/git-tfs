using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sep.Git.Tfs.Test.TestHelpers
{
    public static class ExtensionMethods
    {
        public static void AssertStartsWith(this string s, string expected)
        {
            var start = s.Substring(0, expected.Length);
            Assert.AreEqual(expected, start, "beginning of string");
        }

        public static void AssertEndsWith(this string s, string expected)
        {
            var end = s.Substring(s.Length - expected.Length);
            Assert.AreEqual(expected, end, "end of string");
        }

        public static int MakeArgsAndRun(this GitTfsCommand command, params string [] args)
        {
            return command.Run(args);
        }

        public static void WithFixForHelpColumnWidth(this object o, Action a)
        {
            var originalOS = Environment.GetEnvironmentVariable("OS");
            Environment.SetEnvironmentVariable("OS", "was: " + originalOS);
            try
            {
                a();
            }
            finally
            {
                Environment.SetEnvironmentVariable("OS", originalOS);
            }
        }
    }
}
