using System;
using System.IO;
using System.Linq;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Util;
using Xunit;

namespace Sep.Git.Tfs.Test.Util
{
    public class CommitSpecificCheckinOptionsFactoryTests
    {
        [Fact]
        public void Sets_commit_message_as_checkin_comments()
        {
            TextWriter writer = new StringWriter();
            CommitSpecificCheckinOptionsFactory factory = new CommitSpecificCheckinOptionsFactory(writer);

            string originalCheckinComment = "command-line input";
            CheckinOptions singletonCheckinOptions = new CheckinOptions()
            {
                CheckinComment = originalCheckinComment
            };

            string commitMessage =
@"test message

		formatted git commit message";

            var specificCheckinOptions = factory.BuildCommitSpecificCheckinOptions(singletonCheckinOptions, commitMessage);
            Assert.Equal(commitMessage, specificCheckinOptions.CheckinComment);
        }

        [Fact]
        public void Adds_work_item_to_associate_and_removes_checkin_command_comment()
        {
            StringWriter textWriter = new StringWriter();
            CommitSpecificCheckinOptionsFactory factory = new CommitSpecificCheckinOptionsFactory(textWriter);

            CheckinOptions singletonCheckinOptions = new CheckinOptions();

            string commitMessage =
@"test message

		formatted git commit message

		git-tfs-work-item: 1234 associate";

            string expectedCheckinComment =
@"test message

		formatted git commit message

		";

            var specificCheckinOptions = factory.BuildCommitSpecificCheckinOptions(singletonCheckinOptions, commitMessage);

            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }

        [Fact]
        public void Adds_work_item_to_resolve_and_removes_checkin_command_comment()
        {
            StringWriter textWriter = new StringWriter();
            CommitSpecificCheckinOptionsFactory factory = new CommitSpecificCheckinOptionsFactory(textWriter);

            CheckinOptions singletonCheckinOptions = new CheckinOptions();

            string commitMessage =
@"test message

		formatted git commit message

		git-tfs-work-item: 1234 resolve";

            string expectedCheckinComment =
@"test message

		formatted git commit message

		";

            var specificCheckinOptions = factory.BuildCommitSpecificCheckinOptions(singletonCheckinOptions, commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToResolve.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToResolve);
            Assert.Equal(expectedCheckinComment.Replace(Environment.NewLine, "NEWLINE"), specificCheckinOptions.CheckinComment.Replace(Environment.NewLine, "NEWLINE"));
        }

        [Fact]
        public void Adds_code_reviewer_and_removes_checkin_command_comment()
        {
            StringWriter textWriter = new StringWriter();
            CommitSpecificCheckinOptionsFactory factory = new CommitSpecificCheckinOptionsFactory(textWriter);

            CheckinOptions checkinOptions = new CheckinOptions();

            string commitMessage =
                "Test message\n" +
                "\n" +
                "Some more information,\n" +
                "in a paragraph.\n" +
                "\n" +
                "git-tfs-code-reviewer: John Smith";

            string expectedCheckinComment =
                "Test message\n" +
                "\n" +
                "Some more information,\n" +
                "in a paragraph.";

            var specificCheckinOptions = factory.BuildCommitSpecificCheckinOptions(checkinOptions, commitMessage);
            Assert.Equal(1, specificCheckinOptions.CheckinNotes.Count);
            Assert.Equal("John Smith", specificCheckinOptions.CheckinNotes["Code Reviewer"]);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }
    }
}