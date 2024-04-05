using System.Diagnostics;

namespace GitTfs.Util
{
    public class TemporaryFileStream : FileStream
    {
        public static TemporaryFileStream Acquire()
        {
            var temp = Path.GetTempFileName();
            return new TemporaryFileStream(temp);
        }

        private string _filename;

        public TemporaryFileStream(string filename)
            : base(filename, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            // no need to check filename for null as base constructor would have thrown already in this case
            _filename = filename;
        }

        public string Filename => _filename;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_filename == null) return;

            // doing the same both on disposing and finalizing
            try
            {
                File.Delete(_filename);
                _filename = null;
            }
            catch (IOException e)
            {
                Trace.WriteLine("Unable to delete temp file: " + e);
                // ignore!
            }
            catch (UnauthorizedAccessException e)
            {
                Trace.WriteLine("Unable to delete temp file - unauthorized access: " + e);
                // ignore!
            }
            // other exceptions indicate bugs so shouldn't be catched
        }
    }
}
