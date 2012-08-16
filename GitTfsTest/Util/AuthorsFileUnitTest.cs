using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Sep.Git.Tfs.Util;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Util
{
    /// <summary>
    /// Summary description for AuthorsFileUnitTest
    /// </summary>
    [TestClass]
    public class AuthorsFileUnitTest
    {
        public AuthorsFileUnitTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }


        [TestMethod]
        public void TestEmptyFile()
        {
            MemoryStream ms = new MemoryStream();
            StreamReader sr = new StreamReader(ms);
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(sr);
            Assert.IsNotNull(authFile.Authors);
            Assert.AreEqual<int>(0, authFile.Authors.Count);
        }

        [TestMethod]
        public void TestSimpleRecord()
        {
            string author = @"Domain\Test.User = Test User <TestUser@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.IsNotNull(authFile.Authors);
            Assert.AreEqual<int>(1, authFile.Authors.Count);
            Assert.IsTrue(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Author auth = authFile.Authors[@"Domain\Test.User"];
            Assert.AreEqual<string>("Test User", auth.Name);
            Assert.AreEqual<string>("TestUser@example.com", auth.Email);
        }

        [TestMethod]
        public void TestCaseInsensitiveRecord()
        {
            string author = @"DOMAIN\Test.User = Test User <TestUser@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.IsNotNull(authFile.Authors);
            Assert.AreEqual<int>(1, authFile.Authors.Count);
            Assert.IsTrue(authFile.Authors.ContainsKey(@"domain\Test.User"));
            Author auth = authFile.Authors[@"domain\Test.User"];
            Assert.AreEqual<string>("Test User", auth.Name);
            Assert.AreEqual<string>("TestUser@example.com", auth.Email);
        }

        [TestMethod]
        public void TestMultiLineRecord()
        {
            string author = 
@"Domain\Test.User = Test User <TestUser@example.com>
Domain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.IsNotNull(authFile.Authors);
            Assert.AreEqual<int>(2, authFile.Authors.Count);
            
            Assert.IsTrue(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Author auth = authFile.Authors[@"Domain\Test.User"];
            Assert.AreEqual<string>("Test User", auth.Name);
            Assert.AreEqual<string>("TestUser@example.com", auth.Email);
            
            Assert.IsTrue(authFile.Authors.ContainsKey(@"Domain\Different.User"));
            auth = authFile.Authors[@"Domain\Different.User"];
            Assert.AreEqual<string>("Three Name User", auth.Name);
            Assert.AreEqual<string>(" TestUser@example.com ", auth.Email);
        }

        [TestMethod]
        [ExpectedException(typeof(GitTfsException))]
        public void TestMultiLineRecordWithBlankLine()
        {
            string author =
@"Domain\Test.User = Test User <TestUser@example.com>

Domain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
        }

        [TestMethod]
        [ExpectedException(typeof(GitTfsException))]
        public void TestBadRecord()
        {
            string author =
@"Domain\Test.User = Test User";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
        }
    }
}
