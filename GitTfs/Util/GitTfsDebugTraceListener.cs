using System;
using System.Diagnostics;
using System.IO;

namespace Sep.Git.Tfs.Util
{
    public class GitTfsDebugTraceListener : TraceListener
    {
        static DateTime _launch = DateTime.Now;
        TextWriter _stdout;

        public GitTfsDebugTraceListener(TextWriter stdout)
        {
            _stdout = stdout;
        }

        public override void Write(string message)
        {
            _stdout.Write("[{0}] {1}", DateTime.Now - _launch, message);
        }

        public override void WriteLine(string message)
        {
            _stdout.WriteLine("[{0}] {1}", DateTime.Now - _launch, message);
        }
    }

}
