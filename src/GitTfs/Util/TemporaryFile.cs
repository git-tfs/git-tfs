using System.Diagnostics;

namespace GitTfs.Util
{
    public class TemporaryFile : IDisposable
    {
        public TemporaryFile() : this(System.IO.Path.GetTempFileName())
        {
        }

        public TemporaryFile(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }

        public static implicit operator string (TemporaryFile f)
        {
            return f.Path;
        }

        public void Dispose()
        {
            try
            {
                if (Path != null && File.Exists(Path))
                    File.Delete(Path);
                Path = null;
            }
            catch (Exception e)
            {
                Trace.WriteLine("[TemporaryFile] Unable to remove " + Path + ": " + e);
            }
        }
    }
}
