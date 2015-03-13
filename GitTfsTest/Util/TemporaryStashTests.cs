using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Mocks;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;
using StructureMap.AutoMocking;
using Xunit;

namespace Sep.Git.Tfs.Test.Util
{
    public class TemporaryStashTests
    {
        private RhinoAutoMocker<TemporaryStash> _mocks;

        public TemporaryStashTests()
        {
            _mocks = new RhinoAutoMocker<TemporaryStash>();
        }
        
        [Fact]
        public void On_Consutruction_It_Executes_Git_Stash()
        {
            var repo = _mocks.Get<IGitRepository>();
            repo.Stub(r => r.WorkingCopyHasUnstagedOrUncommitedChanges).Return(true);

            using (new TemporaryStash(repo))
            {
                // Do some git stuff
            }

            repo.AssertWasCalled(r => r.CommandNoisy("stash"));
        }

        [Fact]
        public void On_Dispose_It_Executes_Git_Stash_Pop()
        {
            var repo = _mocks.Get<IGitRepository>();
            repo.Stub(r => r.WorkingCopyHasUnstagedOrUncommitedChanges).Return(true);

            var stash = new TemporaryStash(repo);

            ((IDisposable)stash).Dispose();

            repo.AssertWasCalled(r => r.CommandNoisy("stash", "pop"));
        }

        [Fact]
        public void If_Working_Copy_Is_Not_Dirty_Dont_Stash()
        {
            var repo = _mocks.Get<IGitRepository>();
            repo.Stub(r => r.WorkingCopyHasUnstagedOrUncommitedChanges).Return(false);

            using (new TemporaryStash(repo))
            {
                
            }

            repo.AssertWasNotCalled(r => r.CommandNoisy("stash"));
            repo.AssertWasNotCalled(r => r.CommandNoisy("stash", "pop"));
        }

        [Fact]
        public void If_Working_Copy_Status_Changes_In_The_Middle_That_Stash_Is_Still_Dropped()
        {
            var repo = _mocks.Get<IGitRepository>();
            repo.Stub(r => r.WorkingCopyHasUnstagedOrUncommitedChanges).Return(false);

            using (new TemporaryStash(repo))
            {
                repo.Stub(r => r.WorkingCopyHasUnstagedOrUncommitedChanges).Return(true);
            }

            repo.AssertWasNotCalled(r => r.CommandNoisy("stash"));
            repo.AssertWasNotCalled(r => r.CommandNoisy("stash", "pop"));
        }
    }
}
