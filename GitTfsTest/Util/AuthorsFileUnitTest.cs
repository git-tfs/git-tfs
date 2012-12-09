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
        public void AuthorsFileEmptyFile()
        {
            MemoryStream ms = new MemoryStream();
            StreamReader sr = new StreamReader(ms);
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(sr);
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(0, authFile.Authors.Count);
        }

        [Fact]
        public void AuthorsFileSimpleRecord()
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
        public void AuthorsFileCaseInsensitiveRecord()
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
        public void AuthorsFileMultiLineRecord()
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
        public void AuthorsFileMultiLineRecordWithBlankLine()
        {
            string author =
@"Domain\Test.User = Test User <TestUser@example.com>

Domain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            Assert.Throws<GitTfsException>(() => authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author)))));
        }

        [Fact]
        public void AuthorsFileTestBadRecord()
        {
            string author =
@"Domain\Test.User = Test User";
            AuthorsFile authFile = new AuthorsFile();
            Assert.Throws<GitTfsException>(() => authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author)))));
        }

        [Fact]
        public void AuthorsFileCommentCharacterStartOfLine()
        {
            string author =
@"Domain\Test.User = Test User <TestUser@example.com>
#Domain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(1, authFile.Authors.Count);

            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Assert.False(authFile.Authors.ContainsKey(@"Domain\Different.User"));
        }

        [Fact]
        public void AuthorsFileCommentCharacterMiddleOfLine()
        {
            string author =
@"Domain\Test.User = Test User <TestUser@example.com>
D#omain\Different.User = Three Name User < TestUser@example.com >";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(2, authFile.Authors.Count);

            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Assert.True(authFile.Authors.ContainsKey(@"D#omain\Different.User"));
        }

        [Fact]
        public void AuthorsFileInternationalCharacters()
        {
            string author = @"DOMAIN\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(1, authFile.Authors.Count);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Blåbærsyltetøy"));
            Author auth = authFile.Authors[@"domain\Blåbærsyltetøy"];
            Assert.Equal<string>("ÆØÅ User", auth.Name);
            Assert.Equal<string>("ÆØÅ@example.com", auth.Email);
        }

        [Fact]
        public void AuthorsFileInternationalCharactersMultiLine()
        {
            string author = @"DOMAIN\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>
differentDomain\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(2, authFile.Authors.Count);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Blåbærsyltetøy"));
            Author auth = authFile.Authors[@"domain\Blåbærsyltetøy"];
            Assert.Equal<string>("ÆØÅ User", auth.Name);
            Assert.Equal<string>("ÆØÅ@example.com", auth.Email);

            Assert.True(authFile.Authors.ContainsKey(@"differentDomain\Blåbærsyltetøy"));
            auth = authFile.Authors[@"differentDomain\Blåbærsyltetøy"];
            Assert.Equal<string>("ÆØÅ User", auth.Name);
            Assert.Equal<string>("ÆØÅ@example.com", auth.Email);
        }

        [Fact]
        public void AuthorsFileInternationalCharactersCommented()
        {
            string author = @"DOMAIN\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>
#DifferentDomain\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Equal<int>(1, authFile.Authors.Count);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Blåbærsyltetøy"));
        }
    }
}
