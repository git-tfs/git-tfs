using System;
using System.Diagnostics;
using System.IO;

namespace Sep.Git.Tfs.Util
{
    class TemporaryFile : IDisposable
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
    }
}
