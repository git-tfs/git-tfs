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
            public BaseFixture()
            {
                Remote.Stub(r => r.GetPathInGitRepo(null))
                    .Constraints(Is.Anything())
                    .Do(new Function<string, string>(path => path.Replace("$/Project/", "")));
                Remote.Stub(r => r.ShouldSkip(null))
                    .Constraints(Is.Anything())
                    .Do(new Function<string,bool>(s => s.Contains("ignored")));
            }

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

            protected void AssertChange(IChangeApplicator change, ChangeType type, string gitPath)
            {
                Assert.Equal(type, change.Type);
                Assert.Equal(gitPath, change.GitPath);
            }
        }

        class FakeChange : IChange, IItem
        {
            public static IChange Add(string serverItem)
            {
                return new FakeChange(TfsChangeType.Add, TfsItemType.File, serverItem);
            }

            public static IChange Delete(string serverItem)
            {
                return new FakeChange(TfsChangeType.Delete, TfsItemType.File, serverItem);
            }

            public static IChange Rename(string serverItem, string from, int deletionId = 0)
            {
                return new FakeChange(TfsChangeType.Rename, TfsItemType.File, serverItem, deletionId);
            }

            public static IChange AddDir(string serverItem)
            {
                return new FakeChange(TfsChangeType.Add, TfsItemType.Folder, serverItem);
            }

            TfsChangeType _tfsChangeType;
            TfsItemType _tfsItemType;
            string _serverItem;
            int _deletionId;

            public FakeChange(TfsChangeType tfsChangeType, TfsItemType itemType, string serverItem)
                : this(tfsChangeType, itemType, serverItem, 0)
            {
            }

            public FakeChange(TfsChangeType tfsChangeType, TfsItemType itemType, string serverItem, int deletionId)
            {
                _tfsChangeType = tfsChangeType;
                _tfsItemType = itemType;
                _serverItem = serverItem;
                _deletionId = deletionId;
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
                get { return _deletionId; ; }
            }

            TfsItemType IItem.ItemType
            {
                get { return _tfsItemType; }
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
            public void HasEmptyChangesToApplyOld()
            {
                Assert.Empty(Subject.ChangesToApply());
            }

            [Fact]
            public void HasEmptyChangesToApply()
            {
                Assert.Empty(Subject.ChangesToApply2());
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

            [Fact]
            public void SplitsRenamesAndPutsDeletesFirst()
            {
                var toApply = Subject.ChangesToApply2().ToArray();
                Assert.Equal(6, toApply.Length);
                AssertChange(toApply[0], ChangeType.Delete, "file2.txt");
                AssertChange(toApply[1], ChangeType.Delete, "file4.txt");
                AssertChange(toApply[2], ChangeType.Delete, "oldfile5.txt");
                AssertChange(toApply[3], ChangeType.Update, "file1.txt");
                AssertChange(toApply[4], ChangeType.Update, "file3.txt");
                AssertChange(toApply[5], ChangeType.Update, "file5.txt");
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

        public class SkipDeletedThings : Base<SkipDeletedThings.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange [] {
                        FakeChange.Rename("$/Project/file1.txt", from: "$/Project/oldfile1.txt", deletionId: 33),
                    };
                }
            }

            [Fact]
            public void DoesNotApplyDeletedRenamedFile()
            {
                Assert.Empty(Subject.ChangesToApply2());
            }
        }

        public class DirsAndPathsOutsideTheProject : Base<DirsAndPathsOutsideTheProject.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        FakeChange.AddDir("$/Project/dir1"),
                        FakeChange.AddDir("$/Project2/outsidefile.txt"),
                        FakeChange.Rename("$/Project2/movedoutside.txt", from: "$/Project/startedinside.txt"),
                        FakeChange.Rename("$/Project/movedinside.txt", from: "$/Project2/startedoutside.txt"),
                    };
                }
            }

            [Fact]
            public void DoesNotFetchFilesOutside()
            {
                Assert.Equal(new string[] { "$/Project/movedinside.txt" }, Subject.ChangesToFetch().Select(c => c.Item.ServerItem));
            }

            [Fact]
            public void OnlyAppliesChangesInsideTheProject()
            {
                var toApply = Subject.ChangesToApply2().ToArray();
                Assert.Equal(2, toApply.Length);
                AssertChange(toApply[0], ChangeType.Delete, "startedinside.txt");
                AssertChange(toApply[1], ChangeType.Update, "movedinside.txt");
            }
        }
    }
}
