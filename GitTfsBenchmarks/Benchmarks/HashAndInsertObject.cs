using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using SEP.Extensions;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Benchmarks
{
    class HashAndInsertObject
    {
        class TestGitObject
        {
            public string ObjectId { get; set; }
            public string Contents { get; set; }
            public string ObjectPath { get { return ".git/objects/" + ObjectId.Substring(0, 2) + "/" + ObjectId.Substring(2); } }
        }

        private static readonly List<TestGitObject> TestObjects = new List<TestGitObject>
                                                                    {
                                                                        new TestGitObject
                                                                            {
                                                                                ObjectId = "0e44708cb3166a9f6c5c0a038bc7b2c0c2435e13",
                                                                                Contents = "teststring\r\nanother line\rafter just r\nafter just n"
                                                                            }
                                                                    };
        private static readonly GitHelpers gitHelper = new GitHelpers(TextWriter.Null, null);

        #region WithExecGit

        [Benchmark]
        public static void WithExecGit()
        {
            Run("git", HashWithGit);
        }

        public static string HashWithGit(Stream file)
        {
            // Write the data to a file and insert that, so that git will handle any
            // EOL and encoding issues.
            using (var tempStream = TemporaryFileStream.Acquire())
            {
                file.CopyTo(tempStream);
                return HashWithGit(tempStream.Filename);
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

        #region WithLibGit2Sharp

        [Benchmark]
        public static void WithLibGit2Sharp()
        {
            Run("gitsharp", HashWithLibGit2Sharp);
        }

        private static string HashWithLibGit2Sharp(Stream file)
        {
            var repository = new Repository(Environment.CurrentDirectory);
            var temp = new FileStream(Path.GetTempFileName(),FileMode.Open);
            file.CopyTo(temp);
            return repository.ObjectDatabase.CreateBlob(temp.Name).Sha.ToString();
        }

        #endregion

        #region setup & teardown

        private static string originalCd;

        public static void Reset()
        {
            originalCd = Environment.CurrentDirectory;
        }

        public static void Cleanup()
        {
            Environment.CurrentDirectory = originalCd;
        }

        public static void Check()
        {
            foreach (var testObject in TestObjects)
            {
                if (!File.Exists(testObject.ObjectPath))
                {
                    throw new Exception("Expected file " + testObject.ObjectPath + " was not found!");
                }
                try
                {
                    var objectContents = gitHelper.Command("show", testObject.ObjectId);
                    if(objectContents != testObject.Contents)
                    {
                        throw new Exception("Expected object " + testObject.ObjectId + " to be " + testObject.Contents.Inspect() +
                                            " but it was " + objectContents.Inspect());
                    }
                }
                catch (GitCommandException e)
                {
                    throw new Exception("Unable to read object " + testObject.ObjectId + ".", e);
                }
            }
        }

        private static void Run(string name, Func<Stream, string> hashAndStore)
        {
            var dirName = Path.Combine(Environment.CurrentDirectory, "hash-object-" + name);
            if(Directory.Exists(dirName)) Directory.Delete(dirName, true);
            Directory.CreateDirectory(dirName);
            Environment.CurrentDirectory = dirName;

            gitHelper.CommandNoisy("init");
            foreach (var testObject in TestObjects)
            {
                for (int i = 0; i < 300; i++)
                {
                    hashAndStore(MakeMemoryStream(testObject.Contents));
                }
            }
        }

        private static Stream MakeMemoryStream(string s)
        {
            return new MemoryStream(Encoding.ASCII.GetBytes(s));
        }

        #endregion
    }
}
