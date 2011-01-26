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

        /// <summary>
        /// CSharpOptParse tries to wrap the help text to zero columns when
        /// it writes it during tests, which is not desired. This closure tricks
        /// it into not wrapping the text at all.
        /// </summary>
        public static void FixHelpFormatter(this object o, Action a)
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
