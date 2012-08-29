﻿using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Test.TestHelpers;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Commands
{
    public class ShelveTest
    {
        private RhinoAutoMocker<Shelve> mocks;

        public ShelveTest()
        {
            mocks = new RhinoAutoMocker<Shelve>();
            mocks.Inject<TextWriter>(new StringWriter());
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
        }

        [Fact]
        public void ShouldFailWithLessThanOneParents()
        {
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits("my-head")).Return(new TfsChangesetInfo[0]);

            Assert.NotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldFailWithMoreThanOneParents()
        {
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
            var parentChangesets = new TfsChangesetInfo() {Remote = mocks.Get<IGitTfsRemote>()};
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits("my-head"))
                .Return(new[] {parentChangesets, parentChangesets});

            Assert.NotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldFailWithMoreThanOneParentsWhenSpecifiedParentIsNotAParent()
        {
            var globals = mocks.Get<Globals>();
            globals.Repository = mocks.Get<IGitRepository>();
            globals.UserSpecifiedRemoteId = "wrong-choice";
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits("my-head"))
                .Return(new[] { ChangesetForRemote("ok-choice"), ChangesetForRemote("good-choice") });

            Assert.NotEqual(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldSucceedWithMoreThanOneParentsWhenCorrectParentSpecified()
        {
            var globals = mocks.Get<Globals>();
            globals.Repository = mocks.Get<IGitRepository>();
            globals.UserSpecifiedRemoteId = "good-choice";
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits("my-head"))
                .Return(new[] { ChangesetForRemote("ok-choice"), ChangesetForRemote("good-choice") });

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care", "my-head"));
        }

        [Fact]
        public void ShouldSucceedForOneArgument()
        {
            mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(mocks.Get<IGitTfsRemote>());
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits(null)).IgnoreArguments()
                .Return(new[] {new TfsChangesetInfo {Remote = mocks.Get<IGitTfsRemote>()}});

            Assert.Equal(GitTfsExitCodes.OK, mocks.ClassUnderTest.Run("don't care"));
        }

        [Fact]
        public void ShouldAskForCorrectParent()
        {
            var remote = mocks.Get<IGitTfsRemote>();
            //mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(remote);
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits("commit_to_shelve"))
                .Return(new[] { new TfsChangesetInfo { Remote = mocks.Get<IGitTfsRemote>() } });

            mocks.ClassUnderTest.Run("shelveset name", "commit_to_shelve");
        }

        [Fact]
        public void ShouldTellRemoteToShelve()
        {
            var remote = mocks.Get<IGitTfsRemote>();
            //mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(remote);
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits(null)).IgnoreArguments()
                .Return(new[] { new TfsChangesetInfo { Remote = mocks.Get<IGitTfsRemote>() } });

            mocks.ClassUnderTest.Run("shelveset name");

            remote.AssertWasCalled(x => x.Shelve(null, null, null, false),
                                   y => y.Constraints(Is.Equal("shelveset name"), Is.Equal("HEAD"), Is.Anything(), Is.Anything()));
        }

        [Fact]
        public void ShouldTellRemoteToShelveTreeish()
        {
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
            var remote = mocks.Get<IGitTfsRemote>();
            //mocks.Get<IGitRepository>().Stub(x => x.ReadTfsRemote(null)).IgnoreArguments().Return(remote);
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits(null)).IgnoreArguments()
                .Return(new[] {new TfsChangesetInfo {Remote = remote}});

            mocks.ClassUnderTest.Run("shelveset name", "treeish");

            remote.AssertWasCalled(x => x.Shelve(null, null, null, false),
                                   y => y.Constraints(Is.Equal("shelveset name"), Is.Equal("treeish"), Is.Anything(), Is.Anything()));
        }

        [Fact]
        public void FailureCodeWhenShelvesetExists()
        {
            WireUpMockRemote();
            CreateShelveset("shelveset name");

            var exitCode = mocks.ClassUnderTest.Run("shelveset name", "treeish");
            Assert.Equal(GitTfsExitCodes.ForceRequired, exitCode);
        }

        [Fact]
        public void DoesNotTryToShelveIfShelvesetExists()
        {
            WireUpMockRemote();
            CreateShelveset("shelveset name");

            mocks.ClassUnderTest.Run("shelveset name", "treeish");

            mocks.Get<IGitTfsRemote>().AssertWasNotCalled(
                x => x.Shelve(Arg<string>.Is.Anything, Arg<string>.Is.Anything, Arg<TfsChangesetInfo>.Is.Anything, Arg<bool>.Is.Anything));
        }

        [Fact]
        public void DoesNotStopIfForceIsSpecified()
        {
            mocks.Get<CheckinOptions>().Force = true;
            WireUpMockRemote();
            CreateShelveset("shelveset name");

            mocks.ClassUnderTest.Run("shelveset name", "treeish");

            mocks.Get<IGitTfsRemote>().AssertWasCalled(
                x => x.Shelve(Arg<string>.Is.Anything, Arg<string>.Is.Anything, Arg<TfsChangesetInfo>.Is.Anything, Arg<bool>.Is.Anything));
        }

        private TfsChangesetInfo ChangesetForRemote(string remoteId)
        {
            var mockRemote = mocks.AddAdditionalMockFor<IGitTfsRemote>();
            mockRemote.Stub(x => x.Id).Return(remoteId);
            return new TfsChangesetInfo() {Remote = mockRemote};
        }

        private void WireUpMockRemote()
        {
            mocks.Get<Globals>().Repository = mocks.Get<IGitRepository>();
            var remote = mocks.Get<IGitTfsRemote>();
            mocks.Get<IGitRepository>().Stub(x => x.GetLastParentTfsCommits(null)).IgnoreArguments()
                .Return(new[] { new TfsChangesetInfo { Remote = remote } });
        }

        private void CreateShelveset(string shelvesetName)
        {
            mocks.Get<IGitTfsRemote>().Stub(x => x.HasShelveset(shelvesetName)).Return(true);
        }
    }
}
