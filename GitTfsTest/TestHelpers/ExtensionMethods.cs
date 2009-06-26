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
    }
}
