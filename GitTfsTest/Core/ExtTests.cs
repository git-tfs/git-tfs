using System;
using System.Collections;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Core
{
    [TestClass]
    public class ExtTests
    {
        #region Action.And()

        [TestMethod]
        public void ShouldCombineActionsInOne_And_Call()
        {
            var action1 = new Action<IDictionary>(d => d["action1"] = true);
            var action2 = new Action<IDictionary>(d => d["action2"] = true);
            var action3 = new Action<IDictionary>(d => d["action3"] = true);
            var combinedAction = action1.And(action2, action3);
            var record = new Hashtable();
            combinedAction(record);
            Assert.AreEqual(true, record["action1"], "action1 should have been executed.");
            Assert.AreEqual(true, record["action2"], "action2 should have been executed.");
            Assert.AreEqual(true, record["action3"], "action3 should have been executed.");
        }

        [TestMethod]
        public void ShouldCombineActionsInAChainOfAndCalls()
        {
            var action1 = new Action<IDictionary>(d => d["action1"] = true);
            var action2 = new Action<IDictionary>(d => d["action2"] = true);
            var action3 = new Action<IDictionary>(d => d["action3"] = true);
            var combinedAction = action1.And(action2).And(action3);
            var record = new Hashtable();
            combinedAction(record);
            Assert.AreEqual(true, record["action1"], "action1 should have been executed.");
            Assert.AreEqual(true, record["action2"], "action2 should have been executed.");
            Assert.AreEqual(true, record["action3"], "action3 should have been executed.");
        }

        #endregion

        #region ProcessStartInfo.SetArguments()

        [TestMethod]
        public void ShouldSetProcessStartInfoArguments()
        {
            var info = new ProcessStartInfo();
            info.SetArguments("a", "b", "c");
            Assert.AreEqual("a b c", info.Arguments);
        }

        [TestMethod]
        public void ShouldQuoteSpacesInProcessStartInfoArguments()
        {
            var info = new ProcessStartInfo();
            info.SetArguments("a", "b c");
            Assert.AreEqual("a \"b c\"", info.Arguments);
        }

        #endregion
    }
}