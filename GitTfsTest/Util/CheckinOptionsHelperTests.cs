using System;
using System.IO;
using System.Linq;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Util;
using Xunit;

namespace Sep.Git.Tfs.Test.Util
{
    public class CheckinOptionsHelperTests
    {
        [Fact]
        public void Update_preserves_original_commit()
        {
            TextWriter writer = new StringWriter();
            CheckinOptionsHelper helper = new CheckinOptionsHelper(writer);

            string originalCheckinComment = "command-line input";
            CheckinOptions checkinOptions = new CheckinOptions()
            {
                CheckinComment = originalCheckinComment
            };

            string commitMessage =
@"test message

		formatted git commit message";

            string expectedCheckinComment =
@"test message

		formatted git commit message";

            using (var caretaker = helper.UpdateCheckinOptionsForThisCommit(checkinOptions, commitMessage))
            {
                Assert.Equal(expectedCheckinComment, checkinOptions.CheckinComment);
            }

            Assert.Equal(originalCheckinComment, checkinOptions.CheckinComment);
        }

        [Fact]
        public void Update_associates_and_clears_work_items()
        {
            StringWriter textWriter = new StringWriter();
            CheckinOptionsHelper helper = new CheckinOptionsHelper(textWriter);

            CheckinOptions checkinOptions = new CheckinOptions();

            string commitMessage =
@"test message

		formatted git commit message

		git-tfs-work-item: 1234 associate";

            using (var caretaker = helper.UpdateCheckinOptionsForThisCommit(checkinOptions, commitMessage))
            {
                Assert.Equal(1, checkinOptions.WorkItemsToAssociate.Count);
                Assert.Equal("1234", checkinOptions.WorkItemsToAssociate.First());
            }

            Assert.Equal(0, checkinOptions.WorkItemsToAssociate.Count);
            Assert.Equal("Associating with work item 1234" + textWriter.NewLine, textWriter.ToString());
        }

        [Fact]
        public void Update_resolves_and_clears_work_items()
        {
            StringWriter textWriter = new StringWriter();
            CheckinOptionsHelper helper = new CheckinOptionsHelper(textWriter);

            CheckinOptions checkinOptions = new CheckinOptions();

            string commitMessage =
@"test message

		formatted git commit message

		git-tfs-work-item: 1234 resolve";

            using (var caretaker = helper.UpdateCheckinOptionsForThisCommit(checkinOptions, commitMessage))
            {
                Assert.Equal(1, checkinOptions.WorkItemsToResolve.Count);
                Assert.Equal("1234", checkinOptions.WorkItemsToResolve.First());
            }

            Assert.Equal(0, checkinOptions.WorkItemsToResolve.Count);
            Assert.Equal("Resolving work item 1234" + textWriter.NewLine, textWriter.ToString());
        }
    }
}