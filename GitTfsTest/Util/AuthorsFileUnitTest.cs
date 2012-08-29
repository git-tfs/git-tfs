using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Sep.Git.Tfs.Util;
using Sep.Git.Tfs.Core;
using Xunit;

namespace Sep.Git.Tfs.Test.Util
{
    public class AuthorsFileUnitTest
    {
        [Fact]
        public void TestEmptyFile()
        {
            MemoryStream ms = new MemoryStream();
            StreamReader sr = new StreamReader(ms);
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(sr);
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(0, authFile.Authors.Count);
        }

        [Fact]
        public void TestSimpleRecord()
        {
            string author = @"Domain\Test.User = Test User <TestUser@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(1, authFile.Authors.Count);
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Author auth = authFile.Authors[@"Domain\Test.User"];
            Assert.Equal<string>("Test User", auth.Name);
            Assert.Equal<string>("TestUser@example.com", auth.Email);
        }

        [Fact]
        public void TestCaseInsensitiveRecord()
        {
            string author = @"DOMAIN\Test.User = Test User <TestUser@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(1, authFile.Authors.Count);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Test.User"));
            Author auth = authFile.Authors[@"domain\Test.User"];
            Assert.Equal<string>("Test User", auth.Name);
            Assert.Equal<string>("TestUser@example.com", auth.Email);
        }

        [Fact]
        public void TestMultiLineRecord()
        {
            string author = 
@"Domain\Test.User = Test User <TestUser@example.com>
Domain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(2, authFile.Authors.Count);
            
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Author auth = authFile.Authors[@"Domain\Test.User"];
            Assert.Equal<string>("Test User", auth.Name);
            Assert.Equal<string>("TestUser@example.com", auth.Email);
            
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Different.User"));
            auth = authFile.Authors[@"Domain\Different.User"];
            Assert.Equal<string>("Three Name User", auth.Name);
            Assert.Equal<string>(" TestUser@example.com ", auth.Email);
        }

        [Fact]
        public void TestMultiLineRecordWithBlankLine()
        {
            string author =
@"Domain\Test.User = Test User <TestUser@example.com>

Domain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            Assert.Throws<GitTfsException>(() => authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author)))));
        }

        [Fact]
        public void TestBadRecord()
        {
            string author =
@"Domain\Test.User = Test User";
            AuthorsFile authFile = new AuthorsFile();
            Assert.Throws<GitTfsException>(() => authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author)))));
        }
    }
}
