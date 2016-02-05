using System.IO;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Util
{
    public class ShelveSpecificCheckinOptionsFactoryTests
    {
        private RhinoAutoMocker<ShelveSpecificCheckinOptionsFactory> mocks;

        public ShelveSpecificCheckinOptionsFactoryTests()
        {
            mocks = new RhinoAutoMocker<ShelveSpecificCheckinOptionsFactory>();
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

            var specificCheckinOptions = GetShelveSpecificCheckinOptions().BuildShelveSetSpecificCheckinOptions(new CheckinOptions(), commitMessage);

            Assert.Equal(1, specificCheckinOptions.WorkItemsToAssociate.Count);
            Assert.Contains("1234", specificCheckinOptions.WorkItemsToAssociate);
            Assert.Equal(expectedCheckinComment, specificCheckinOptions.CheckinComment);
        }

        private ShelveSpecificCheckinOptionsFactory GetShelveSpecificCheckinOptions()
        {
            return new ShelveSpecificCheckinOptionsFactory(new StringWriter(), mocks.Get<Globals>());
        }
    }
}
