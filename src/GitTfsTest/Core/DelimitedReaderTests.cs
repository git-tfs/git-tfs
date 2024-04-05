using System.Text;

using GitTfs.Core;

using Xunit;

namespace GitTfs.Test.Core
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
            Assert.Null(reader.Read());
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
            Assert.Null(reader.Read());
        }
    }
}
