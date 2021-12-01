using System;
using System.Collections.Generic;
using System.IO;
using GitTfs.Commands;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using Xunit;
using System.Diagnostics;
using Moq;

namespace GitTfs.Test.Core
{
    public class TfsWorkspaceTests : BaseTest, IDisposable
    {
        private TfsWorkspace tfsWorkspace;
        private Mock<IWorkspace> workspace;
        CheckinOptions checkinOptions = new CheckinOptions();

        public TfsWorkspaceTests()
        {
            workspace = new Mock<IWorkspace>();
            string localDirectory = string.Empty;
            TfsChangesetInfo contextVersion = new Mock<TfsChangesetInfo>().Object;
            var remoteMock = new Mock<IGitTfsRemote>();
            remoteMock.SetupAllProperties();
            remoteMock.SetupGet(x => x.Repository).Returns(new Mock<IGitRepository>().Object);
            IGitTfsRemote remote = remoteMock.Object;
            ITfsHelper tfsHelper = new Mock<ITfsHelper>().Object;
            CheckinPolicyEvaluator policyEvaluator = new CheckinPolicyEvaluator();

            tfsWorkspace = new TfsWorkspace(workspace.Object, localDirectory, contextVersion, remote, checkinOptions,
                tfsHelper, policyEvaluator);
        }


        [Fact]
        public void Nothing_to_checkin()
        {
            workspace.Setup(w => w.GetPendingChanges()).Returns((IPendingChange[]) null);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(checkinOptions);
            });

            Assert.Equal("Nothing to checkin!", ex.Message);
        }

        [Fact]
        public void Checkin_failed()
        {
            IPendingChange pendingChange = new Mock<IPendingChange>().Object;
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Setup(w => w.GetPendingChanges()).Returns(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult();

            workspace.Setup(w => w.EvaluateCheckin(
                                    It.IsAny<TfsCheckinEvaluationOptions>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>()))
                    .Returns(checkinEvaluationResult);

            workspace.Setup(w => w.Checkin(
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>(),
                                    It.IsAny<TfsPolicyOverrideInfo>(),
                                    It.IsAny<bool>()))
                      .Returns(0);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(checkinOptions);
            });

            Assert.Equal("Checkin failed!", ex.Message);
        }

        [Fact]
        public void Policy_failed()
        {
            var logger = new StringWriter();
            Trace.Listeners.Add(new TextWriterTraceListener(logger));

            IPendingChange pendingChange = new Mock<IPendingChange>().Object;
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Setup(w => w.GetPendingChanges()).Returns(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult()
                        .WithPoilicyFailure("No work items associated.");

            workspace.Setup(w => w.EvaluateCheckin(
                                    It.IsAny<TfsCheckinEvaluationOptions>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>()))
                    .Returns(checkinEvaluationResult);

            workspace.Setup(w => w.Checkin(
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>(),
                                    It.IsAny<TfsPolicyOverrideInfo>(),
                                    It.IsAny<bool>()))
                      .Returns(0);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(checkinOptions);
            });

            Assert.Equal("No changes checked in.", ex.Message);
            Assert.Contains("[ERROR] Policy: No work items associated.", logger.ToString());
        }

        [Fact]
        public void Policy_failed_and_Force_without_an_OverrideReason()
        {
            var logger = new StringWriter();
            Trace.Listeners.Add(new TextWriterTraceListener(logger));

            IPendingChange pendingChange = new Mock<IPendingChange>().Object;
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Setup(w => w.GetPendingChanges()).Returns(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult()
                        .WithPoilicyFailure("No work items associated.");

            checkinOptions.Force = true;

            workspace.Setup(w => w.EvaluateCheckin(
                                    It.IsAny<TfsCheckinEvaluationOptions>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>()))
                    .Returns(checkinEvaluationResult);

            workspace.Setup(w => w.Checkin(
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>(),
                                    It.IsAny<TfsPolicyOverrideInfo>(),
                                    It.IsAny<bool>()))
                      .Returns(0);

            var ex = Assert.Throws<GitTfsException>(() =>
            {
                var result = tfsWorkspace.Checkin(checkinOptions);
            });

            Assert.Equal("A reason must be supplied (-f REASON) to override the policy violations.", ex.Message);
            Assert.Contains("[ERROR] Policy: No work items associated.", logger.ToString());
        }

        [Fact]
        public void Policy_failed_and_Force_with_an_OverrideReason()
        {
            var logger = new StringWriter();
            Trace.Listeners.Add(new TextWriterTraceListener(logger));

            IPendingChange pendingChange = new Mock<IPendingChange>().Object;
            IPendingChange[] allPendingChanges = new IPendingChange[] { pendingChange };
            workspace.Setup(w => w.GetPendingChanges()).Returns(allPendingChanges);

            ICheckinEvaluationResult checkinEvaluationResult =
                new StubbedCheckinEvaluationResult()
                        .WithPoilicyFailure("No work items associated.");

            checkinOptions.Force = true;
            checkinOptions.OverrideReason = "no work items";

            workspace.Setup(w => w.EvaluateCheckin(
                                    It.IsAny<TfsCheckinEvaluationOptions>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>()))
                    .Returns(checkinEvaluationResult);

            workspace.Setup(w => w.Checkin(
                                    It.IsAny<IPendingChange[]>(),
                                    It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<ICheckinNote>(),
                                    It.IsAny<IEnumerable<IWorkItemCheckinInfo>>(),
                                    It.IsAny<TfsPolicyOverrideInfo>(),
                                    It.IsAny<bool>())).Returns(1);

            var result = tfsWorkspace.Checkin(checkinOptions);

            Assert.Contains("[OVERRIDDEN] Policy: No work items associated.", logger.ToString());
        }

        public void Dispose()
        {
            Trace.Listeners.Clear(); ;
        }
    }
}
