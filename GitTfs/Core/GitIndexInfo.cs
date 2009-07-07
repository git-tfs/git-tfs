using System;
using System.IO;

namespace Sep.Git.Tfs.Core
{
    class GitIndexInfo : IDisposable
    {
        public static int Do(IGitHelpers repository, Action<GitIndexInfo> indexAction)
        {
            int nr = 0;
            repository.CommandInputPipe(stdin => nr = Do(stdin, indexAction),
                "update-index", "-z", "--index-info");
            return nr;
        }

        private static int Do(TextWriter stdin, Action<GitIndexInfo> action)
        {
            using (var indexInfo = new GitIndexInfo(stdin))
            {
                action(indexInfo);
                return indexInfo.nr;
            }
        }

        private readonly TextWriter stdin;
        private int nr = 0;

        private GitIndexInfo(TextWriter stdin)
        {
            this.stdin = stdin;
        }

        public int Remove(string path)
        {
            stdin.Write("0 ");
            stdin.Write(new string('0', 40));
            stdin.Write('\t');
            stdin.Write(path);
            stdin.Write('\0');
            return ++nr;
        }

        public int Update(string mode, string hash, string path)
        {
            stdin.Write(mode);
            stdin.Write(' ');
            stdin.Write(hash);
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
