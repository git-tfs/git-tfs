using System;
using System.Collections.Generic;
using System.Linq;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;
using Xunit;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;

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
            protected IChange[] Changes { get { return Fixture.Changeset.Changes; } }

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

        class FakeChange : IChange, IItem
        {
            public static IChange Add(string serverItem)
            {
                return new FakeChange(TfsChangeType.Add, serverItem);
            }

            public static IChange Delete(string serverItem)
            {
                return new FakeChange(TfsChangeType.Delete, serverItem);
            }

            public static IChange Rename(string serverItem, string from)
            {
                return new FakeChange(TfsChangeType.Rename, serverItem);
            }

            TfsChangeType _tfsChangeType;
            string _serverItem;

            public FakeChange(TfsChangeType tfsChangeType, string serverItem)
            {
                _tfsChangeType = tfsChangeType;
                _serverItem = serverItem;
            }

            TfsChangeType IChange.ChangeType
            {
                get { return _tfsChangeType; }
            }

            IItem IChange.Item
            {
                get { return this; }
            }

            IVersionControlServer IItem.VersionControlServer
            {
                get { throw new NotImplementedException(); }
            }

            int IItem.ChangesetId
            {
                get { throw new NotImplementedException(); }
            }

            string IItem.ServerItem
            {
                get { return _serverItem; }
            }

            int IItem.DeletionId
            {
                get { throw new NotImplementedException(); }
            }

            TfsItemType IItem.ItemType
            {
                get { throw new NotImplementedException(); }
            }

            int IItem.ItemId
            {
                get { throw new NotImplementedException(); }
            }

            long IItem.ContentLength
            {
                get { throw new NotImplementedException(); }
            }

            TemporaryFile IItem.DownloadFile()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        public class WithNoChanges : Base<WithNoChanges.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
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

        public class WithAddsAndDeletes : Base<WithAddsAndDeletes.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        FakeChange.Add("$/Project/file1.txt"),
                        FakeChange.Delete("$/Project/file2.txt"),
                        FakeChange.Add("$/Project/file3.txt"),
                        FakeChange.Delete("$/Project/file4.txt"),
                        FakeChange.Rename("$/Project/file5.txt", from: "$/Project/oldfile5.txt"),
                    };
                    Remote.Stub(r => r.GetPathInGitRepo(null))
                        .Constraints(Is.Anything())
                        .Do(new Function<string, string>(path => path.Replace("$/Project/", "")));
                    Remote.Stub(r => r.ShouldSkip(null))
                        .Constraints(Is.Anything())
                        .Return(false);
                }
            }

            [Fact]
            public void HasChanges()
            {
                Assert.True(Subject.HasChanges());
            }

            [Fact]
            public void FetchesAllChanges()
            {
                Assert.Equal(5, Subject.ChangesToFetch().Count());
            }

            [Fact]
            public void AppliesDeletesFirst()
            {
                var toApply = Subject.ChangesToApply();
                Assert.Equal(new string [] {
                    "$/Project/file2.txt",
                    "$/Project/file4.txt",
                    "$/Project/file5.txt",
                    "$/Project/file1.txt",
                    "$/Project/file3.txt",
                }, toApply.Select(change => change.Item.ServerItem).ToArray());
            }
        }

        public class WithIgnoredThings : Base<WithIgnoredThings.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        FakeChange.Add("$/Project/0-ignored.txt"),
                        FakeChange.Delete("$/Project/1-ignored.txt"),
                        FakeChange.Add("$/Project/2-included.txt"),
                        FakeChange.Delete("$/Project/3-included.txt"),
                        FakeChange.Rename("$/Project/4-ignored.txt", from: "$/Project/4-wasignored.txt"),
                        FakeChange.Rename("$/Project/5-ignored.txt", from: "$/Project/5-wasincluded.txt"),
                        FakeChange.Rename("$/Project/6-included.txt", from: "$/Project/6-wasignored.txt"),
                    };
                    Remote.Stub(r => r.GetPathInGitRepo(null))
                        .Constraints(Is.Anything())
                        .Do(new Function<string, string>(path => path.Replace("$/Project/", "")));
                    Remote.Stub(r => r.ShouldSkip(null))
                        .Constraints(Is.Anything())
                        .Do(new Function<string,bool>(s => s.Contains("ignored")));
                }
            }

            [Fact]
            public void HasChanges()
            {
                Assert.True(Subject.HasChanges());
            }

            [Fact]
            public void FetchesAllExceptIgnored()
            {
                var fetchChanges = Subject.ChangesToFetch().ToArray();
                Assert.Equal(3, fetchChanges.Length);
                Assert.Contains(Changes[2], fetchChanges);
                Assert.Contains(Changes[3], fetchChanges);
                Assert.Contains(Changes[6], fetchChanges);
            }

            [Fact]
            public void AppliesDeletesFirst()
            {
                var toApply = Subject.ChangesToApply();
                Assert.Equal(new string [] {
                    "$/Project/3-included.txt",
                    "$/Project/6-included.txt",
                    "$/Project/2-included.txt",
                }, toApply.Select(change => change.Item.ServerItem).ToArray());
            }
        }
    }
}
