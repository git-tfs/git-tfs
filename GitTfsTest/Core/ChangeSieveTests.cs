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

        // Sets up a ChangeSieve for testing.
        public abstract class BaseFixture
        {
            public BaseFixture()
            {
                // Make this remote act like it's mapped to $/Project
                Remote.Stub(r => r.GetPathInGitRepo(null))
                    .Constraints(Is.Anything())
                    .Do(new Function<string, string>(path => path != null && path.StartsWith("$/Project/") ? path.Replace("$/Project/", "") : null));
                // Make this remote ignore any path that includes "ignored".
                Remote.Stub(r => r.ShouldSkip(null))
                    .Constraints(Is.Anything())
                    .Do(new Function<string,bool>(s => s.Contains("ignored")));
            }

            private ChangeSieve _changeSieve;
            public ChangeSieve Subject
            {
                get { return _changeSieve ?? (_changeSieve = new ChangeSieve(Changeset, new PathResolver(Remote, "", InitialTree))); }
            }

            private Dictionary<string, GitObject> _initialTree;
            public virtual Dictionary<string, GitObject> InitialTree
            {
                get { return _initialTree ?? (_initialTree = new Dictionary<string, GitObject>(StringComparer.InvariantCultureIgnoreCase)); }
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

                public void Get(ITfsWorkspace workspace, IEnumerable<IChange> changes, Action<Exception> ignorableErrorHandler)
                {
                    throw new NotImplementedException();
                }
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

        // A base class for ChangeSieve test classes.
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

            protected void AssertChanges(IEnumerable<ApplicableChange> actualChanges, params ApplicableChange[] expectedChanges)
            {
                Assert.Equal(Stringify(expectedChanges), Stringify(actualChanges));
            }

            string Stringify(IEnumerable<ApplicableChange> changes)
            {
                return string.Join("\n", changes.Select(c => "" + c.Type + ":" + c.GitPath));
            }
        }

        // A stub IChange implementation.
        class FakeChange : IChange, IItem, IVersionControlServer
        {
            public static IChange Add(string serverItem, TfsChangeType additionalChange = 0, int deletionId = 0)
            {
                return new FakeChange(TfsChangeType.Add | additionalChange, TfsItemType.File, serverItem, deletionId);
            }

            public static IChange Edit(string serverItem, TfsChangeType additionalChange = 0)
            {
                return new FakeChange(TfsChangeType.Edit | additionalChange, TfsItemType.File, serverItem);
            }

            public static IChange Delete(string serverItem, TfsChangeType additionalChange = 0)
            {
                return new FakeChange(TfsChangeType.Delete | additionalChange, TfsItemType.File, serverItem);
            }

            public static IChange Rename(string serverItem, string from, TfsChangeType additionalChange = 0, int deletionId = 0)
            {
                return new FakeChange(TfsChangeType.Rename | additionalChange, TfsItemType.File, serverItem, deletionId, from);
            }

            public static IChange AddDir(string serverItem, TfsChangeType additionalChange = 0)
            {
                return new FakeChange(TfsChangeType.Add | additionalChange, TfsItemType.Folder, serverItem);
            }

            public static IChange Branch(string serverItem)
            {
                return new FakeChange(TfsChangeType.Branch, TfsItemType.File, serverItem);
            }

            public static IChange Merge(string serverItem)
            {
                return new FakeChange(TfsChangeType.Merge, TfsItemType.File, serverItem);
            }

            public static IChange MergeNewFile(string serverItem)
            {
                return new FakeChange(TfsChangeType.Merge | TfsChangeType.Branch, TfsItemType.File, serverItem);
            }

            public static IChange DeleteDir(string serverItem)
            {
                return new FakeChange(TfsChangeType.Delete, TfsItemType.Folder, serverItem);
            }

            const int ChangesetId = 10;

            TfsChangeType _tfsChangeType;
            TfsItemType _tfsItemType;
            string _serverItem;
            int _deletionId;
            string _renamedFrom;
            int _itemId;
            static int _maxItemId = 0;

            private FakeChange(TfsChangeType tfsChangeType, TfsItemType itemType, string serverItem, int deletionId = 0, string renamedFrom = null)
            {
                _tfsChangeType = tfsChangeType;
                _tfsItemType = itemType;
                _serverItem = serverItem;
                _deletionId = deletionId;
                _renamedFrom = renamedFrom;
                _itemId = ++_maxItemId;
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
                get { return this; }
            }

            int IItem.ChangesetId
            {
                get { return FakeChange.ChangesetId; }
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
                get { return _itemId; }
            }

            long IItem.ContentLength
            {
                get { throw new NotImplementedException(); }
            }

            TemporaryFile IItem.DownloadFile()
            {
                throw new NotImplementedException();
            }

            IItem IVersionControlServer.GetItem(int itemId, int changesetNumber)
            {
                if (itemId == _itemId && changesetNumber == ChangesetId - 1 && _tfsChangeType.HasFlag(TfsChangeType.Rename))
                    return new PreviousItem(_renamedFrom);
                throw new NotImplementedException();
            }

            class PreviousItem : IItem
            {
                string _oldName;

                public PreviousItem(string oldName)
                {
                    _oldName = oldName;
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
                    get { return _oldName; }
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

            IItem IVersionControlServer.GetItem(string itemPath, int changesetNumber)
            {
                throw new NotImplementedException();
            }

            IItem[] IVersionControlServer.GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType)
            {
                throw new NotImplementedException();
            }

            IEnumerable<IChangeset> IVersionControlServer.QueryHistory(string path, int version, int deletionId, TfsRecursionType recursion, string user, int versionFrom, int versionTo, int maxCount, bool includeChanges, bool slotMode, bool includeDownloadInfo)
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
            public void HasEmptyChangesToFetch()
            {
                Assert.Empty(Subject.GetChangesToFetch());
            }

            [Fact]
            public void HasEmptyChangesToApply()
            {
                AssertChanges(Subject.GetChangesToApply() /* expect an empty list */);
            }
        }

        public class WithAddsAndDeletes : Base<WithAddsAndDeletes.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        /*0*/FakeChange.Add("$/Project/file1.txt"),
                        /*1*/FakeChange.Delete("$/Project/file2.txt"),
                        /*2*/FakeChange.Add("$/Project/file3.txt"),
                        /*3*/FakeChange.Delete("$/Project/file4.txt"),
                        /*4*/FakeChange.Rename("$/Project/file5.txt", from: "$/Project/oldfile5.txt"),
                    };
                }
            }

            [Fact]
            public void FetchesAllChanges()
            {
                var fetchChanges = Subject.GetChangesToFetch().ToArray();
                Assert.Equal(5, fetchChanges.Length);
                Assert.Contains(Changes[0], fetchChanges);
                Assert.Contains(Changes[1], fetchChanges);
                Assert.Contains(Changes[2], fetchChanges);
                Assert.Contains(Changes[3], fetchChanges);
                Assert.Contains(Changes[4], fetchChanges);
            }

            [Fact]
            public void SplitsRenamesAndPutsDeletesFirst()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("file2.txt"),
                    ApplicableChange.Delete("file4.txt"),
                    ApplicableChange.Delete("oldfile5.txt"),
                    ApplicableChange.Update("file1.txt"),
                    ApplicableChange.Update("file3.txt"),
                    ApplicableChange.Update("file5.txt"));
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
            public void FetchesAllExceptIgnored()
            {
                var fetchChanges = Subject.GetChangesToFetch().ToArray();
                Assert.Equal(3, fetchChanges.Length);
                Assert.Contains(Changes[2], fetchChanges);
                Assert.Contains(Changes[3], fetchChanges);
                Assert.Contains(Changes[6], fetchChanges);
            }

            [Fact]
            public void AppliesDeletesFirst()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("1-ignored.txt"),
                    ApplicableChange.Delete("3-included.txt"),
                    ApplicableChange.Delete("4-wasignored.txt"),
                    ApplicableChange.Delete("5-wasincluded.txt"),
                    ApplicableChange.Delete("6-wasignored.txt"),
                    ApplicableChange.Update("2-included.txt"),
                    ApplicableChange.Update("6-included.txt"));
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
                        FakeChange.Add("$/Project/deletedfile1.txt", deletionId: 33), // this seems like nonsense.
                    };
                }
            }

            [Fact]
            public void DoesNotApplyDeletedRenamedFile()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("oldfile1.txt"));
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
                        FakeChange.Add("$/Project2/outsidefile.txt"),
                        FakeChange.Rename("$/Project2/movedoutside.txt", from: "$/Project/startedinside.txt"),
                        FakeChange.Rename("$/Project/movedinside.txt", from: "$/Project2/startedoutside.txt"),
                    };
                }
            }

            [Fact]
            public void DoesNotFetchFilesOutside()
            {
                Assert.Equal(new string[] { "$/Project/dir1", "$/Project/movedinside.txt" }, Subject.GetChangesToFetch().Select(c => c.Item.ServerItem));
            }

            [Fact]
            public void OnlyAppliesChangesInsideTheProject()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("startedinside.txt"),
                    ApplicableChange.Update("movedinside.txt"));
            }
        }

        public class WithExistingItems : Base<WithExistingItems.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    InitialTree.Add("dir1", new GitObject { Path = "dir1" });
                    InitialTree.Add("dir1/file1.exe", new GitObject { Path = "dir1/file1.exe", Mode = "100755".ToFileMode() });
                    InitialTree.Add("dir1/file4.txt", new GitObject { Path = "dir1/file4.txt", Mode = "100644".ToFileMode() });
                    InitialTree.Add("dir2", new GitObject { Path = "dir2" });
                    InitialTree.Add("dir2/file2.txt", new GitObject { Path = "dir2/file2.txt" });
                    Changeset.Changes = new IChange[] {
                        FakeChange.Add("$/Project/DIR2/file3.txt"),
                        FakeChange.Delete("$/Project/DIR2/FILE2.txt"),
                        FakeChange.Edit("$/Project/dir1/file1.exe"),
                        FakeChange.Edit("$/Project/dir1/file4.txt"),
                    };
                }
            }

            [Fact]
            public void UpdatesPathCasing()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("dir2/file2.txt"),
                    ApplicableChange.Update("dir2/file3.txt"),
                    ApplicableChange.Update("dir1/file1.exe"),
                    ApplicableChange.Update("dir1/file4.txt"));
            }

            [Fact]
            public void PreservesFileMode()
            {
                var toApply = Subject.GetChangesToApply().ToArray();
                Assert.Equal("100644", toApply[1].Mode.ToModeString()); // new file
                Assert.Equal("100755", toApply[2].Mode.ToModeString()); // existing executable file
                Assert.Equal("100644", toApply[3].Mode.ToModeString()); // existing normal file
            }
        }

        public class SkipBranchedThings : Base<SkipBranchedThings.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    InitialTree.Add("file6.txt", new GitObject() { Commit = "SHA" });

                    Changeset.Changes = new[] {
                        /*0*/FakeChange.Branch("$/Project/file6.txt"), // Do not include, because it was there before.
                        /*1*/FakeChange.Branch("$/Project/file7.txt"), // Include, because it was not there before.
                        /*2*/FakeChange.Edit("$/Project/file8.txt", TfsChangeType.Branch), // Include, because it's not just branched.
                        /*3*/FakeChange.Rename("$/Project/file9.txt", from: "$/Project/oldfile9.txt", additionalChange: TfsChangeType.Branch), // Include, because it's not just branched.
                    };
                }
            }

            [Fact]
            public void DoesNotFetchBranchedFile()
            {
                var fetchChanges = Subject.GetChangesToFetch().ToArray();
                Assert.Equal(3, fetchChanges.Length); // one is missing
                // Changes[0] (branch of file6.txt) is missing
                Assert.Contains(Changes[1], fetchChanges);
                Assert.Contains(Changes[2], fetchChanges);
                Assert.Contains(Changes[3], fetchChanges);
            }

            [Fact]
            public void DoesNotApplyBranchedFile()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("oldfile9.txt"),
                    ApplicableChange.Update("file7.txt"),
                    ApplicableChange.Update("file8.txt"),
                    ApplicableChange.Update("file9.txt"));
            }
        }

        public class SkipMergedThings : Base<SkipMergedThings.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    InitialTree.Add("file6.txt", new GitObject() { Commit = "SHA" });
                    InitialTree.Add("file7.txt", new GitObject() { Commit = "SHA" });

                    Changeset.Changes = new[] {
                        /*0*/FakeChange.Merge("$/Project/file6.txt"), // Do not include, because it was there before.
                        /*1*/FakeChange.MergeNewFile("$/Project/file7.txt"),  // Do not include, because it was in branch before.
                        /*2*/FakeChange.Merge("$/Project/file8.txt"), // Include, because it was not there before.
                        /*3*/FakeChange.Edit("$/Project/file9.txt", TfsChangeType.Merge), // Include, because it's not just branched.
                        /*4*/FakeChange.Rename("$/Project/file10.txt", from: "$/Project/oldfile10.txt", additionalChange: TfsChangeType.Merge), // Include, because it's not just branched.
                    };
                }
            }

            [Fact]
            public void DoesNotFetchBranchedFile()
            {
                var fetchChanges = Subject.GetChangesToFetch().ToArray();
                Assert.Equal(3, fetchChanges.Length); // one is missing
                // Changes[0] (branch of file6.txt) is missing
                // Changes[1] (branch of file7.txt) is missing
                Assert.Contains(Changes[2], fetchChanges);
                Assert.Contains(Changes[3], fetchChanges);
                Assert.Contains(Changes[4], fetchChanges);
            }

            [Fact]
            public void DoesNotApplyBranchedFile()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Delete("oldfile10.txt"),
                    ApplicableChange.Update("file8.txt"),
                    ApplicableChange.Update("file9.txt"),
                    ApplicableChange.Update("file10.txt"));
            }
        }
       
        public class WithDeleteMainFolderBranchAndSubItems : Base<WithDeleteMainFolderBranchAndSubItems.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        FakeChange.Delete("$/Project/file1.txt"),
                        FakeChange.Delete("$/Project/file2.txt"),
                        FakeChange.Delete("$/Project/file3.txt"),
                        FakeChange.DeleteDir("$/Project/"),
                        FakeChange.Delete("$/Project/file4.txt"),
                        FakeChange.Delete("$/Project/file5.txt"),
                    };
                }
            }

            [Fact]
            public void WhenMainBranchFolderIsDeleted_ThenKeepFileInGitCommitByDoingNothing()
            {
                Assert.Equal(0, Subject.GetChangesToApply().Count());
            }

            [Fact]
            public void DoNotFetch()
            {
                // Because we're not going to apply changes, don't waste time fetching any.
                Assert.Equal(0, Subject.GetChangesToFetch().Count());
            }
        }

        public class WithDeleteOtherFolder : Base<WithDeleteOtherFolder.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        FakeChange.Edit("$/Project/file1.txt"),
                        FakeChange.DeleteDir("$/Projec"),
                        FakeChange.Delete("$/Projec/file.txt"),
                        FakeChange.DeleteDir("$/Project2"),
                        FakeChange.Delete("$/Project2/file.txt"),
                    };
                }
            }

            [Fact]
            public void IncludesChangesInThisProject()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Update("file1.txt"));
            }

            [Fact]
            public void FetchesChangesInThisProject()
            {
                Assert.Equal(new string[] { "$/Project/file1.txt" }, Subject.GetChangesToFetch().Select(c => c.Item.ServerItem));
            }
        }

        public class RenamedFromDeleted : Base<RenamedFromDeleted.Fixture>
        {
            public class Fixture : BaseFixture
            {
                public Fixture()
                {
                    Changeset.Changes = new IChange[] {
                        new RenamedFromDeletedChange("$/Project/file1.txt"),
                    };
                }
            }

            [Fact]
            public void FetchesItemRenamedAfterDelete()
            {
                AssertChanges(Subject.GetChangesToApply(),
                    ApplicableChange.Update("file1.txt"));
            }

            // A Change/Item that only throws an exception when you try to query its history.
            public class RenamedFromDeletedChange : IChange, IItem, IVersionControlServer
            {
                // This is the interesting part of this implementation.
                // The TFS client throws an exception of type Microsoft.TeamFoundation.VersionControl.Client.ItemNotFoundException.
                // ChangeSieve doesn't have a reference to the TFS client libs, so it can't catch that exact exception.
                // This class, therefore, throws an exception that is of a type that ChangeSieve can't specifically catch.
                IEnumerable<IChangeset> IVersionControlServer.QueryHistory(string path, int version, int deletionId, TfsRecursionType recursion, string user, int versionFrom, int versionTo, int maxCount, bool includeChanges, bool slotMode, bool includeDownloadInfo)
                {
                    throw new AnExceptionTypeThatYouCannotCatch();
                }

                class AnExceptionTypeThatYouCannotCatch : Exception
                {
                }

                // The rest of the implementation is pretty straight-forward.

                readonly string _serverItem;

                // Accept a name so that the name is more obviously matched between the Fixture
                // and the assertion.
                public RenamedFromDeletedChange(string serverItem)
                {
                    _serverItem = serverItem;
                }

                TfsChangeType IChange.ChangeType
                {
                    get { return TfsChangeType.Rename; }
                }

                IItem IChange.Item
                {
                    get { return this; }
                }

                IVersionControlServer IItem.VersionControlServer
                {
                    get { return this; }
                }

                int IItem.ChangesetId
                {
                    get { return 100; }
                }

                string IItem.ServerItem
                {
                    get { return _serverItem; }
                }

                int IItem.DeletionId
                {
                    get { return 0; }
                }

                TfsItemType IItem.ItemType
                {
                    get { return TfsItemType.File; }
                }

                int IItem.ItemId
                {
                    get { return 200; }
                }

                long IItem.ContentLength
                {
                    get { return 0; }
                }

                TemporaryFile IItem.DownloadFile()
                {
                    return null;
                }

                IItem IVersionControlServer.GetItem(int itemId, int changesetNumber)
                {
                    return null;
                }

                IItem IVersionControlServer.GetItem(string itemPath, int changesetNumber)
                {
                    return null;
                }

                IItem[] IVersionControlServer.GetItems(string itemPath, int changesetNumber, TfsRecursionType recursionType)
                {
                    return null;
                }
            }
        }
    }
}
