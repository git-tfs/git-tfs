using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace GitTfs.Test.Util
{
    public class ShelveSpecificCheckinOptionsFactoryTests
    {
        private readonly MoqAutoMocker<CheckinOptionsFactory> mocks;

        public ShelveSpecificCheckinOptionsFactoryTests()
        {
            mocks = new MoqAutoMocker<CheckinOptionsFactory>();
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
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

            var specificCheckinOptions = GetCheckinOptionsFactory().BuildShelveSetSpecificCheckinOptions(new CheckinOptions(), commitMessage);

            Assert.Single(specificCheckinOptions.WorkItemsToAssociate);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }

        private CheckinOptionsFactory GetCheckinOptionsFactory() => new CheckinOptionsFactory(mocks.Get<Globals>());
    }
}
