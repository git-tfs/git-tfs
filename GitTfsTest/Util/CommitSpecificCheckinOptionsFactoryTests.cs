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
        private CommitSpecificCheckinOptionsFactory GetCommitSpecificCheckinOptions()
        {
            return new CommitSpecificCheckinOptionsFactory(new StringWriter(), new Globals(), new AuthorsFile());
        }

        [Fact]
        public void Sets_commit_message_as_checkin_comments()
        {
            string originalCheckinComment = "command-line input";
            var singletonCheckinOptions = new CheckinOptions
            {
                CheckinComment = originalCheckinComment
            };

            string commitMessage = @"test message

		formatted git commit message";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(singletonCheckinOptions, commitMessage);
            Assert.Equal(commitMessage, specificCheckinOptions.CheckinComment);
        }

        [Fact]
        public void Adds_work_item_to_associate_and_removes_checkin_command_comment()
        {
            string commitMessage = @"test message

		formatted git commit message

		git-tfs-work-item: 1234 associate";

            string expectedCheckinComment = @"test message

		formatted git commit message

		";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);

            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }

        [Fact]
        public void Checkin_regex_does_not_require_action()
        {
            string commitMessage = @"test message
                formatted git commit message

                git-tfs-work-item: 1234";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
        }

        [Fact]
        public void Checkin_regex_with_hash()
        {
            string commitMessage = @"test workitem #5676";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("5676", specificCheckinOptions.WorkItemsToAssociate);
        }

        [Fact]
        public void Checkin_regex_with_hash2()
        {
            string commitMessage = @"test workitem #56p76";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("56", specificCheckinOptions.WorkItemsToAssociate);
        }

        [Fact]
        public void Checkin_regex_with_hash_wrong_format()
        {
            string commitMessage = @"test workitem #f5676";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(0, specificCheckinOptions.WorkItemsToAssociate.Count);
        }

        [Fact]
        public void Checkin_regex_with_hash_2_styles()
        {
            string commitMessage = @"test workitem #5676 1 only
                git-tfs-work-item: 1234";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(2, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Contains("5676", specificCheckinOptions.WorkItemsToAssociate);
        }

        [Fact]
        public void Checkin_regex_with_hash_same_workitems()
        {
            string commitMessage = @"test workitem #5676
                git-tfs-work-item: 5676";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("5676", specificCheckinOptions.WorkItemsToAssociate);
        }

        [Fact]
        public void Adds_work_item_to_resolve_and_removes_checkin_command_comment()
        {
            string commitMessage = @"test message

		formatted git commit message

		git-tfs-work-item: 1234 resolve";

            string expectedCheckinComment = @"test message

		formatted git commit message

		";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToResolve.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToResolve);
            Assert.Equal(expectedCheckinComment.Replace(Environment.NewLine, "NEWLINE"), specificCheckinOptions.CheckinComment.Replace(Environment.NewLine, "NEWLINE"));
        }

        [Fact]
        public void Adds_multiple_work_items_and_removes_checkin_command_comment()
        {
            string commitMessage = @"test message

		formatted git commit message

		git-tfs-work-item: 1234 resolve
        git-tfs-work-item: 5678 associate

";

            string expectedCheckinComment = @"test message

		formatted git commit message

		";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToResolve.Count);
            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToResolve);
            Assert.Contains("5678", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Equal(expectedCheckinComment.Replace(Environment.NewLine, "NEWLINE"), specificCheckinOptions.CheckinComment.Replace(Environment.NewLine, "NEWLINE"));
        }

        [Fact]
        public void Adds_reviewers_and_removes_checkin_command_comment()
        {
            string commitMessage =
                "Test message\n" +
                "\n" +
                "Some more information,\n" +
                "in a paragraph.\n" +
                "\n" +
                "git-tfs-code-reviewer: John Smith\n" +
                "git-tfs-security-reviewer: Teddy Knox\n" +
                "git-tfs-performance-reviewer: Liam Fasterson";

            string expectedCheckinComment =
                "Test message\n" +
                "\n" +
                "Some more information,\n" +
                "in a paragraph.";

            var specificCheckinOptions = GetCommitSpecificCheckinOptions().BuildCommitSpecificCheckinOptions(new CheckinOptions(), commitMessage);
            Assert.Equal(3, specificCheckinOptions.CheckinNotes.Count);
            Assert.Equal("John Smith", specificCheckinOptions.CheckinNotes["Code Reviewer"]);
            Assert.Equal("Teddy Knox", specificCheckinOptions.CheckinNotes["Security Reviewer"]);
            Assert.Equal("Liam Fasterson", specificCheckinOptions.CheckinNotes["Performance Reviewer"]);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }
    }
}