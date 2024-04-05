namespace GitTfs.Core
{
    public class DelimitedReader
    {
        private readonly TextReader _reader;

        public DelimitedReader(TextReader reader)
        {
            _reader = reader;
            Delimiter = "\0";
        }

        public string Delimiter { get; set; }

        public string Read()
        {
            if (-1 == _reader.Peek()) return null;
            var nextString = "";
            int nextChar;
            while (-1 != (nextChar = _reader.Read()))
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
