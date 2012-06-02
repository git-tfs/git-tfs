using System;
using System.Diagnostics;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    public class GitIndexInfo : IDisposable
    {
        public static int Do(IGitRepository repository, Action<GitIndexInfo> indexAction)
        {
            int nr = 0;
            repository.CommandInputPipe(stdin => nr = Do(stdin, repository, indexAction),
                "update-index", "-z", "--index-info");
            return nr;
        }

        private static int Do(TextWriter stdin, IGitRepository repository, Action<GitIndexInfo> action)
        {
            using (var indexInfo = new GitIndexInfo(stdin, repository))
            {
                action(indexInfo);
                return indexInfo.nr;
            }
        }

        private readonly TextWriter stdin;
        private readonly IGitRepository repository;
        private int nr = 0;

        private GitIndexInfo(TextWriter stdin, IGitRepository repository)
        {
            this.stdin = stdin;
            this.repository = repository;
        }

        public int Remove(string path)
        {
            Trace.WriteLine("   D " + path);
            stdin.Write("0 ");
            stdin.Write(new string('0', 40));
            stdin.Write('\t');
            stdin.Write(path);
            stdin.Write('\0');
            return ++nr;
        }

        public int Update(string mode, string path, string localFile)
        {
            var sha = repository.HashAndInsertObject(localFile);
            Trace.WriteLine("   U " + sha + " = " + path);
            stdin.Write(mode);
            stdin.Write(' ');
            stdin.Write(sha);
            stdin.Write('\t');
            stdin.Write(path);
            stdin.Write('\0');
            return ++nr;
        }

        public void Dispose()
        {
            stdin.Close();
        }
    }
}
