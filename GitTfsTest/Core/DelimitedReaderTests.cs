using System.Collections.Generic;
using System.IO;
using System.Text;
using Sep.Git.Tfs.Core;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class DelimitedReaderTests : BaseTest
    {
        [Fact]
        public void ShouldParseTwoNullTerminatedStrings()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("abc"));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes("def"));
            bytes.Add(0);
            var reader = new DelimitedReader(new StreamReader(new MemoryStream(bytes.ToArray())));
            Assert.Equal("abc", reader.Read());
            Assert.Equal("def", reader.Read());
            Assert.Equal(null, reader.Read());
        }

        [Fact]
        public void ShouldParseWhenLastStringHasNoTerminator()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("abc"));
            bytes.Add(0);
            bytes.AddRange(Encoding.ASCII.GetBytes("def"));
            var reader = new DelimitedReader(new StreamReader(new MemoryStream(bytes.ToArray())));
            Assert.Equal("abc", reader.Read());
            Assert.Equal("def", reader.Read());
            Assert.Equal(null, reader.Read());
        }
    }
}
