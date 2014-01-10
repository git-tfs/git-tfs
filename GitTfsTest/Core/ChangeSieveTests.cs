using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using Xunit;
using Rhino.Mocks;

namespace Sep.Git.Tfs.Test.Core
{
    public class ChangeSieveTests
    {
        #region Base fixture

        public abstract class BaseFixture
        {
            private ChangeSieve _changeSieve;
            public ChangeSieve Subject
            {
                get { return _changeSieve ?? (_changeSieve = new ChangeSieve(InitialTree, Changeset, Remote)); }
            }

            private Dictionary<string, GitObject> _initialTree;
            public virtual Dictionary<string, GitObject> InitialTree
            {
                get { return _initialTree ?? (_initialTree = new Dictionary<string, GitObject>()); }
            }

            private FakeChangeset _changeset;
            public virtual FakeChangeset Changeset
            {
                get { return _changeset ?? (_changeset = new FakeChangeset()); }
            }
            public class FakeChangeset : IChangeset
            {
                public IVersionControlServer VersionControlServer { get; set; }
                public int ChangesetId { get; set; }
                public string Comment { get; set; }
                public DateTime CreationDate { get; set; }
                public string Committer { get; set; }
                public IChange[] Changes { get; set; }
            }

            private IGitTfsRemote _remote;
            public virtual IGitTfsRemote Remote
            {
                get { return _remote ?? (_remote = BuildRemote()); }
            }
            protected virtual IGitTfsRemote BuildRemote()
            {
                return _mocks.StrictMock<IGitTfsRemote>();
            }

            protected MockRepository _mocks = new MockRepository();
            public MockRepository Mocks { get { return _mocks; } }
        }

        public class Base<FixtureClass> : IDisposable where FixtureClass : BaseFixture, new()
        {
            protected readonly FixtureClass Fixture;
            protected ChangeSieve Subject { get { return Fixture.Subject; } }

            public Base()
            {
                Fixture = new FixtureClass();
                var subject = Fixture.Subject;
                Fixture.Mocks.ReplayAll();
            }

            public void Dispose()
            {
                Fixture.Mocks.VerifyAll();
            }
        }

        #endregion

        [Trait("focus", "true")]
        public class WithNoChanges : Base<WithNoChanges.NoChangesFixture>
        {
            public class NoChangesFixture : BaseFixture
            {
                public NoChangesFixture()
                {
                    Changeset.Changes = new IChange[0];
                }
            }

            [Fact]
            public void HasNoChanges()
            {
                Assert.False(Subject.HasChanges());
            }

            [Fact]
            public void HasEmptyChangesToFetch()
            {
                Assert.Empty(Subject.ChangesToFetch());
            }

            [Fact]
            public void HasEmptyChangesToApply()
            {
                Assert.Empty(Subject.ChangesToApply());
            }
        }
    }
}
