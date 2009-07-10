using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Core
{
    [TestClass]
    public class DelimitedReaderTests
    {
        [TestMethod]
        public void ShouldParseTwoNullTerminatedStrings()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("abc"));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes("def"));
            bytes.Add(0);
            var reader = new DelimitedReader(new StreamReader(new MemoryStream(bytes.ToArray())));
            Assert.AreEqual("abc", reader.Read());
            Assert.AreEqual("def", reader.Read());
            Assert.AreEqual(null, reader.Read());
        }

        [TestMethod]
        public void ShouldParseWhenLastStringHasNoTerminator()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("abc"));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes("def"));
            var reader = new DelimitedReader(new StreamReader(new MemoryStream(bytes.ToArray())));
            Assert.AreEqual("abc", reader.Read());
            Assert.AreEqual("def", reader.Read());
            Assert.AreEqual(null, reader.Read());
        }
    }
}
