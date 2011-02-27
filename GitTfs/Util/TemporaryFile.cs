using System;
using System.Diagnostics;
using System.IO;

namespace Sep.Git.Tfs.Util
{
    public class TemporaryFile : IDisposable
    {
        private string tempFile;

        public TemporaryFile()
        {
            tempFile = Path.GetTempFileName();
        }

        public static implicit operator string(TemporaryFile tempFile)
        {
            return tempFile.tempFile;
        }

        public void Dispose()
        {
            try
            {
                if (tempFile != null) File.Delete(tempFile);
                tempFile = null;
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to delete temp file: " + e);
                // ignore!
            }
        }

        public Stream ToStream()
        {
            return new TemporaryFileStream(this);
        }

        class TemporaryFileStream : Stream
        {
            private readonly TemporaryFile _temporaryFile;
            private FileStream _baseStream;

            public TemporaryFileStream(TemporaryFile temporaryFile)
            {
                _temporaryFile = temporaryFile;
                _baseStream = File.OpenRead(_temporaryFile);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _temporaryFile.Dispose();
            }

            public override void Flush()
            {
                _baseStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _baseStream.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _baseStream.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _baseStream.Write(buffer, offset, count);
            }

            public override bool CanRead
            {
                get { return _baseStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _baseStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _baseStream.CanWrite; }
            }

            public override long Length
            {
                get { return _baseStream.Length; }
            }

            public override long Position
            {
                get { return _baseStream.Position; }
                set { _baseStream.Position = value; }
            }
        }
    }
}
