using System.IO;

namespace Sep.Git.Tfs.Core
{
    public class DelimitedReader
    {
        private readonly TextReader reader;

        public DelimitedReader(TextReader reader)
        {
            this.reader = reader;
            Delimiter = "\0";
        }

        public string Delimiter { get; set; }

        public string Read()
        {
            if (-1 == reader.Peek()) return null;
            var nextString = "";
            int nextChar;
            while (-1 != (nextChar = reader.Read()))
            {
                nextString = nextString + (char)nextChar;
                if (nextString.EndsWith(Delimiter))
                {
                    return nextString.Substring(0, nextString.Length - Delimiter.Length);
                }
            }
            return nextString;
        }
    }
}
