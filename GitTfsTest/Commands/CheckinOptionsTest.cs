using Sep.Git.Tfs.Commands;
using StructureMap.AutoMocking;
using NDesk.Options;
using Xunit;

namespace Sep.Git.Tfs.Test.Commands
{
    public class CheckinOptionsTest
    {
        private RhinoAutoMocker<CheckinOptions> mocks;

        public CheckinOptionsTest()
        {
            mocks = new RhinoAutoMocker<CheckinOptions>();
        }

        [Fact]
        public void ProvideNumericRenameThresholdIsAllowed()
        {
            string[] args = { "checkin", "--rename-threshold=03" };
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("03", mocks.ClassUnderTest.RenameThreshold);
        }

        [Fact]
        public void ProvidePercentageRenameThresholdIsAllowed()
        {
            string[] args = { "checkin", "--rename-threshold=70%" };
            mocks.ClassUnderTest.OptionSet.Parse(args);
            Assert.Equal("70%", mocks.ClassUnderTest.RenameThreshold);
        }

        [Fact]
        public void ProvideInvalidRenameThresholdIsNotAllowed()
        {
            string[] args = { "checkin", "--rename-threshold=ABC" };
            Assert.Throws<OptionException>(() => mocks.ClassUnderTest.OptionSet.Parse(args));
            Assert.Equal(null, mocks.ClassUnderTest.RenameThreshold);
        }


    }
}
