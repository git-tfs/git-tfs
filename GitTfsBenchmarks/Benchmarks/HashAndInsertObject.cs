using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Benchmarks
{
    class HashAndInsertObject
    {
        private static readonly GitHelpers gitHelper = new GitHelpers(TextWriter.Null);

        #region WithPureDotNet

        [Benchmark]
        public static void WithPureDotNet()
        {
            Run(HashWithDotNet);
        }

        private static string HashWithDotNet(Stream data)
        {
            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("blob " + data.Length));
            bytes.Add(0);
            var binaryReader = new BinaryReader(data);
            bytes.AddRange(binaryReader.ReadBytes((int)data.Length));

            var byteArray = bytes.ToArray();
            var sha1 = BitConverter.ToString(SHA1.Create().ComputeHash(byteArray)).Replace("-", "");

            var sha1_0 = sha1.Substring(0, 2);
            var sha1_1 = sha1.Substring(2);
            var objectFile = CombinePaths(".git", "objects", sha1_0, sha1_1);
            if(!File.Exists(objectFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(objectFile));
                using(var output = File.Create(objectFile))
                {
                    using(var deflater = new DeflateStream(output, CompressionMode.Compress))
                    {
                        deflater.Write(byteArray, 0, byteArray.Length);
                    }
                }
            }
            return sha1;
        }

        private static string CombinePaths(string part, params string [] parts)
        {
            foreach(var extraPart in parts)
            {
                part = Path.Combine(part, extraPart);
            }
            return part;
        }

        #endregion

        #region WithExecGit

        [Benchmark]
        public static void WithExecGit()
        {
            Run(HashWithGit);
        }

        public static string HashWithGit(Stream file)
        {
            // Write the data to a file and insert that, so that git will handle any
            // EOL and encoding issues.
            using (var tempFile = new TemporaryFile())
            {
                using (var tempStream = File.Create(tempFile))
                {
                    file.CopyTo(tempStream);
                }
                return HashWithGit(tempFile);
            }
        }

        public static string HashWithGit(string filename)
        {
            string newHash = null;
            gitHelper.CommandOutputPipe(stdout => newHash = stdout.ReadLine().Trim(),
                "hash-object", "-w", filename);
            return newHash;
        }

        #endregion

        #region setup & teardown

        private static string originalCd;
        private static string tempDir;

        public static void Reset()
        {
            originalCd = Environment.CurrentDirectory;

            tempDir = Path.GetTempFileName();
            File.Delete(tempDir);
            Directory.CreateDirectory(tempDir);
            Environment.CurrentDirectory = tempDir;

            gitHelper.CommandNoisy("init");
        }

        public static void Cleanup()
        {
            Environment.CurrentDirectory = originalCd;
            try { Directory.Delete(tempDir, true); } catch { }
        }

        public static void Check()
        {
            foreach (var file in GetExpectedFiles())
            {
                if (!File.Exists(file))
                {
                    throw new Exception("Expected file " + file + " was not found!");
                }
            }
        }

        private static IEnumerable<string> GetExpectedFiles()
        {
            yield return ".git/objects/0e/44708cb3166a9f6c5c0a038bc7b2c0c2435e13";
//            return Split(@".git/config
//.git/description
//.git/HEAD
//.git/hooks/applypatch-msg.sample
//.git/hooks/commit-msg.sample
//.git/hooks/post-commit.sample
//.git/hooks/post-receive.sample
//.git/hooks/post-update.sample
//.git/hooks/pre-applypatch.sample
//.git/hooks/pre-commit.sample
//.git/hooks/pre-rebase.sample
//.git/hooks/prepare-commit-msg.sample
//.git/hooks/update.sample
//.git/info/exclude
//.git/objects/b1/c9ce7741174a512bf18d409495b12f9b194c9b");
        }

        private static IEnumerable<string> Split(string files)
        {
            var regex = new Regex("\\s+");
            return regex.Split(files);
        }

        private static void Run(Func<Stream, string> hashAndStore)
        {
            for (int i = 0; i < 300; i++)
            {
                hashAndStore(
                    MakeMemoryStream("teststring\r\nanother line\rafter just r\nafter just n"));
            }
        }

        private static Stream MakeMemoryStream(string s)
        {
            return new MemoryStream(Encoding.ASCII.GetBytes(s));
        }

        #endregion
    }
}
