using System.Collections;
using System.Diagnostics;

using GitTfs.Core;
using GitTfs.Core.TfsInterop;

using Xunit;

namespace GitTfs.Test.Core
{
    public class ExtTests : BaseTest
    {
        #region Action.And()

        [Fact]
        public void ShouldCombineActionsInOne_And_Call()
        {
            var action1 = new Action<IDictionary>(d => d["action1"] = true);
            var action2 = new Action<IDictionary>(d => d["action2"] = true);
            var action3 = new Action<IDictionary>(d => d["action3"] = true);
            var combinedAction = action1.And(action2, action3);
            var record = new Hashtable();
            combinedAction(record);
            Assert.True((bool)record["action1"], "action1 should have been executed.");
            Assert.True((bool)record["action2"], "action2 should have been executed.");
            Assert.True((bool)record["action3"], "action3 should have been executed.");
        }

        [Fact]
        public void ShouldCombineActionsInAChainOfAndCalls()
        {
            var action1 = new Action<IDictionary>(d => d["action1"] = true);
            var action2 = new Action<IDictionary>(d => d["action2"] = true);
            var action3 = new Action<IDictionary>(d => d["action3"] = true);
            var combinedAction = action1.And(action2).And(action3);
            var record = new Hashtable();
            combinedAction(record);
            Assert.True((bool)record["action1"], "action1 should have been executed.");
            Assert.True((bool)record["action2"], "action2 should have been executed.");
            Assert.True((bool)record["action3"], "action3 should have been executed.");
        }

        #endregion

        #region ProcessStartInfo.SetArguments()

        [Fact]
        public void ShouldSetProcessStartInfoArguments()
        {
            var info = new ProcessStartInfo();
            info.SetArguments("a", "b", "c");
            Assert.Equal("a b c", info.Arguments);
        }

        [Fact]
        public void ShouldQuoteSpacesInProcessStartInfoArguments()
        {
            var info = new ProcessStartInfo();
            info.SetArguments("a", "b c");
            Assert.Equal("a \"b c\"", info.Arguments);
        }

        #endregion

        #region FormatForGit()

        [Fact]
        public void ShouldFormatDateForGit()
        {
            var date = new DateTime(2000, 1, 2, 12, 34, 56);
            Assert.Equal("2000-01-02T12:34:56Z", date.ToLocalTime().FormatForGit());
        }

        #endregion

        #region ChangeType.IncludesOneOf()

        [Fact]
        public void ShouldNotDetectUnincludedChangeTypes() => Assert.False(TfsChangeType.Add.IncludesOneOf(TfsChangeType.Branch, TfsChangeType.Delete, TfsChangeType.Edit, TfsChangeType.Encoding, TfsChangeType.Lock, TfsChangeType.Merge, TfsChangeType.None, TfsChangeType.Rename, TfsChangeType.Undelete));

        [Fact]
        public void ShouldNotDetectUnincludedChangeType()
        {
            var everythingExceptAdd = TfsChangeType.Branch | TfsChangeType.Delete | TfsChangeType.Edit |
                                      TfsChangeType.Encoding | TfsChangeType.Lock | TfsChangeType.Merge |
                                      TfsChangeType.None | TfsChangeType.Rename | TfsChangeType.Undelete;
            Assert.False(everythingExceptAdd.IncludesOneOf(TfsChangeType.Add));
        }

        [Fact]
        public void ShouldDetectIncludedChangeTypeForExactMatch() => Assert.True(TfsChangeType.Add.IncludesOneOf(TfsChangeType.Add));

        [Fact]
        public void ShouldDetectIncludedChangeTypeForOneOfSeveral() => Assert.True(TfsChangeType.Add.IncludesOneOf(TfsChangeType.Branch, TfsChangeType.Add));

        [Fact]
        public void ShouldDetectIncludedChangeTypeForMultivalue() => Assert.True((TfsChangeType.Add | TfsChangeType.Branch).IncludesOneOf(TfsChangeType.Branch));

        [Fact]
        public void ShouldNotDetectMultivaluesThatIntersectWithoutBeingSubset() => Assert.False((TfsChangeType.Add | TfsChangeType.Branch).IncludesOneOf(TfsChangeType.Branch | TfsChangeType.Edit));

        [Fact]
        public void ShouldDetectMultivaluesThatIntersectAndAreASubset() => Assert.True((TfsChangeType.Add | TfsChangeType.Branch | TfsChangeType.Edit).IncludesOneOf(TfsChangeType.Branch | TfsChangeType.Edit));

        #endregion
    }
}
