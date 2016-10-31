using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Util
{
    public class ShelveSpecificCheckinOptionsFactoryTests
    {
        private readonly RhinoAutoMocker<CheckinOptionsFactory> mocks;

        public ShelveSpecificCheckinOptionsFactoryTests()
        {
            mocks = new RhinoAutoMocker<CheckinOptionsFactory>();
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

            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }

        private CheckinOptionsFactory GetCheckinOptionsFactory()
        {
            return new CheckinOptionsFactory(mocks.Get<Globals>());
        }
    }
}
