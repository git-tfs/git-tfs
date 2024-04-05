using System.Text;
using GitTfs.Util;
using GitTfs.Core;
using Xunit;

namespace GitTfs.Test.Util
{
    public class AuthorsFileUnitTest : BaseTest
    {
        [Fact]
        public void AuthorsFileEmptyFile()
        {
            MemoryStream ms = new MemoryStream();
            StreamReader sr = new StreamReader(ms);
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(sr);
            Assert.NotNull(authFile.Authors);
            Assert.Empty(authFile.Authors);
        }

        [Fact]
        public void AuthorsFileSimpleRecord()
        {
            string author = @"Domain\Test.User = Test User <TestUser@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Single(authFile.Authors);
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Author auth = authFile.Authors[@"Domain\Test.User"];
            Assert.Equal("Test User", auth.Name);
            Assert.Equal("TestUser@example.com", auth.Email);
        }

        [Fact]
        public void AuthorsFileCaseInsensitiveRecord()
        {
            string author = @"DOMAIN\Test.User = Test User <TestUser@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.ASCII.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Single(authFile.Authors);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Test.User"));
            Author auth = authFile.Authors[@"domain\Test.User"];
            Assert.Equal("Test User", auth.Name);
            Assert.Equal("TestUser@example.com", auth.Email);
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
            Assert.Equal("Test User", auth.Name);
            Assert.Equal("TestUser@example.com", auth.Email);

            Assert.True(authFile.Authors.ContainsKey(@"Domain\Different.User"));
            auth = authFile.Authors[@"Domain\Different.User"];
            Assert.Equal("Three Name User", auth.Name);
            Assert.Equal(" TestUser@example.com ", auth.Email);
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
            Assert.Single(authFile.Authors);

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
            Assert.Single(authFile.Authors);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Blåbærsyltetøy"));
            Author auth = authFile.Authors[@"domain\Blåbærsyltetøy"];
            Assert.Equal("ÆØÅ User", auth.Name);
            Assert.Equal("ÆØÅ@example.com", auth.Email);
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
            Assert.Equal("ÆØÅ User", auth.Name);
            Assert.Equal("ÆØÅ@example.com", auth.Email);

            Assert.True(authFile.Authors.ContainsKey(@"differentDomain\Blåbærsyltetøy"));
            auth = authFile.Authors[@"differentDomain\Blåbærsyltetøy"];
            Assert.Equal("ÆØÅ User", auth.Name);
            Assert.Equal("ÆØÅ@example.com", auth.Email);
        }

        [Fact]
        public void AuthorsFileInternationalCharactersCommented()
        {
            string author = @"DOMAIN\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>
#DifferentDomain\Blåbærsyltetøy = ÆØÅ User <ÆØÅ@example.com>";
            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(author))));
            Assert.NotNull(authFile.Authors);
            Assert.Single(authFile.Authors);
            Assert.True(authFile.Authors.ContainsKey(@"domain\Blåbærsyltetøy"));
        }


        private string _MergeAuthorsIntoString(string[] authors)
        {
            string result = @"";

            for (int i = 0; i < authors.Length; ++i)
            {
                if (i != 0)
                    result += Environment.NewLine;

                result += authors[i];
            }
            return result;
        }


        private AuthorsFile _SetupAuthorsFile(string[] authors)
        {
            string author = _MergeAuthorsIntoString(authors);

            AuthorsFile authFile = new AuthorsFile();
            authFile.Parse(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(author))));
            return authFile;
        }

        [Fact]
        public void AuthorsFileMultipleUsers()
        {
            string[] authors = {
                @"Domain\Test.User = Test User <TestUser@example.com>",
                @"Domain\Different.User = Three Name User < DiffUser@example.com >",
                @"Domain\Yet.Another.User = Mr. 3 <yau@example.com>"
            };

            AuthorsFile authFile = _SetupAuthorsFile(authors);

            Assert.Equal<int>(authors.Length, authFile.Authors.Count);
            Assert.Equal<int>(authors.Length, authFile.AuthorsByGitUserId.Count);

            // contains all tfs users
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Different.User"));
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Yet.Another.User"));

            // contains all git users
            Assert.True(authFile.AuthorsByGitUserId.ContainsKey(Author.BuildGitUserId("TestUser@example.com")));
            Assert.True(authFile.AuthorsByGitUserId.ContainsKey(Author.BuildGitUserId("DiffUser@example.com")));
            Assert.True(authFile.AuthorsByGitUserId.ContainsKey(Author.BuildGitUserId("YAU@example.com")));
        }


        [Fact]
        public void AuthorsFileDifferentIdsForUsersWithSameEmail()
        {
            string[] authors = {
                @"Domain\Test.User = Test User <TestUser@example.com>",
                @"Domain\Different.User = Three Name User < TestUser@example.com >",
                @"Domain\Yet.Another.User = Mr. 3 <testuser@example.com>"
            };

            AuthorsFile authFile = _SetupAuthorsFile(authors);

            Assert.NotNull(authFile.AuthorsByGitUserId);

            // multiple users with the same email -> 3 tfs users, 1 git user
            Assert.Equal<int>(authors.Length, authFile.Authors.Count);
            Assert.Single(authFile.AuthorsByGitUserId);

            // contains all tfs users
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Test.User"));
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Different.User"));
            Assert.True(authFile.Authors.ContainsKey(@"Domain\Yet.Another.User"));

            // contains all git users
            Assert.True(authFile.AuthorsByGitUserId.ContainsKey(Author.BuildGitUserId("TestUser@example.com")));

            // return the first tfs user id in the authors file when multiple
            // match to the same git id.
            Tuple<string, string> git_author = new Tuple<string, string>("Test User", "TestUser@example.com");
            Author a = authFile.FindAuthor(git_author);
            Assert.NotNull(a);
            Assert.Equal(@"Domain\Test.User", a.TfsUserId);
        }



        [Fact]
        public void AuthorsFileFindAuthors()
        {
            string[] authors = {
                @"Domain\Test.User = Test User <TestUser@example.com>",
                @"Domain\Different.User = Three Name User < TestUser@example.com >",
                @"Domain\Yet.Another.User = Mr. 3 <testuser@example.com>"
            };

            AuthorsFile authFile = _SetupAuthorsFile(authors);

            // find existing author
            Tuple<string, string> gitAuthor = new Tuple<string, string>("Test User", "TestUser@example.com");
            Author existingAuthor = authFile.FindAuthor(gitAuthor);
            Assert.NotNull(existingAuthor);

            // try to find unknown author
            Tuple<string, string> gitUnknownAuthor = new Tuple<string, string>("Test User", "TestFailUser@example.com");
            Author unknownAuthor = authFile.FindAuthor(gitUnknownAuthor);
            Assert.Null(unknownAuthor);
        }
    }
}
