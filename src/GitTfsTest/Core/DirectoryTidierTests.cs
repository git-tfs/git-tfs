using Xunit;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using Moq;

namespace GitTfs.Test.Core
{
    public class DirectoryTidierTests : BaseTest, IDisposable
    {
        private readonly MockRepository mocks;
        private readonly ITfsWorkspaceModifier mockWorkspace;
        private readonly TfsTreeEntry[] initialTfsTree;
        private DirectoryTidier _tidy;

        public DirectoryTidierTests()
        {
            mocks = new MockRepository(MockBehavior.Default);
            mockWorkspace = mocks.OneOf<ITfsWorkspaceModifier>();
            initialTfsTree = new TfsTreeEntry[] {
                item(TfsItemType.Folder, "topDir"),
                item(TfsItemType.File,   "topDir/topFile.txt"),
                item(TfsItemType.Folder, "topDir/midDir"),
                item(TfsItemType.File,   "topDir/midDir/midFile.txt"),
                item(TfsItemType.Folder, "topDir/midDir/bottomDir"),
                item(TfsItemType.File,   "topDir/midDir/bottomDir/file1.txt"),
                item(TfsItemType.File,   "topDir/midDir/bottomDir/file2.txt"),
                item(TfsItemType.Folder, "dirA"),
                item(TfsItemType.Folder, "dirA/dirB"),
                item(TfsItemType.File  , "dirA/dirB/file.txt"),
                item(TfsItemType.File  , "dirA/file.txt"),
                item(TfsItemType.Folder, "dir1"),
                item(TfsItemType.Folder, "dir1/dir2"),
                item(TfsItemType.Folder, "dir1/dir2/dir3"),
                item(TfsItemType.File,   "dir1/dir2/dir3/lonelyFile.txt"),
                item(TfsItemType.File,   "rootFile.txt"),
            };
        }

        public void Dispose() => TidyDisposeToProcess();

        private ITfsWorkspaceModifier Tidy
        {
            get
            {
                if (_tidy == null)
                {
                    _tidy = new DirectoryTidier(mockWorkspace, () => initialTfsTree);
                }
                return _tidy;
            }
        }

        private void TidyDisposeToProcess() => ((IDisposable)Tidy).Dispose();

        [Fact]
        public void PassesThroughGetLocalPath()
        {
            Mock.Get(mockWorkspace).Setup(x => x.GetLocalPath("git-path")).Returns("tfs-path");
            Assert.Equal("tfs-path", Tidy.GetLocalPath("git-path"));
        }

        [Fact]
        public void NoChangesMeansNoChanges() =>
            // nothing!
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();

        [Fact]
        public void AddingAFilePassesThroughAndDoesNotRemoveOtherItems()
        {
            Tidy.Add("topDir/midDir/bottomDir/newFile.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Add("topDir/midDir/bottomDir/newFile.txt"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void RemovingAFileWithSiblingsDoesNotRemoveTheDir()
        {
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void RemovingBothSiblingFilesRemovesTheDir()
        {
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void NoDoubleDelete()
        {
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void RemovingAFileRemovesAllEmptyParents()
        {
            Tidy.Delete("dir1/dir2/dir3/lonelyFile.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("dir1/dir2/dir3/lonelyFile.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("dir1"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }


        [Fact]
        public void SourceDirectoryOfRenameShouldNotBeDeleted()
        {
            // Even though a directory may end up empty after a file has been moved from it,
            // TFS does not allow deleting that empty directory.
            // See https://github.com/git-tfs/git-tfs/issues/313

            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Rename("topDir/midDir/bottomDir/file2.txt", "file2.txt", ScoreIsIrrelevant);

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Rename("topDir/midDir/bottomDir/file2.txt", "file2.txt", ScoreIsIrrelevant));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
            // "topDir/midDir/bottomDir/" becomes empty but is not deleted
        }

        [Fact]
        public void MovingAFileOutLeavesAllEmptyParents()
        {
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteOnlyTheTopOfEmptyDirTree()
        {
            Tidy.Delete("dirA/file.txt");
            Tidy.Delete("dirA/dirB/file.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("dirA/file.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("dirA/dirB/file.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("dirA"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void MovingAFileOutAndInLeavesParents()
        {
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);
            Tidy.Rename("topDir/topFile.txt", "dir1/dir2/dir3/replacement.txt", ScoreIsIrrelevant);

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            Mock.Get(mockWorkspace).Verify(x => x.Rename("topDir/topFile.txt", "dir1/dir2/dir3/replacement.txt", ScoreIsIrrelevant));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void DeletingAFileAndAddingAnotherLeavesParents()
        {
            Tidy.Delete("dir1/dir2/dir3/lonelyFile.txt");
            Tidy.Add("dir1/dir2/dir3/newFile.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("dir1/dir2/dir3/lonelyFile.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Add("dir1/dir2/dir3/newFile.txt"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void DeletingAFileAndAddingAnotherInANewSubdirectoryLeavesParents()
        {
            Tidy.Delete("top/file.txt");
            Tidy.Add("top/sub/file.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("top/file.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Add("top/sub/file.txt"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void DeletingAFileAndAddingAnotherInANewSiblingDirectoryLeavesParent()
        {
            Tidy.Delete("top/sub1/file.txt");
            Tidy.Add("top/sub2/file.txt");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("top/sub1/file.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Add("top/sub2/file.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("top/sub1")); // but NOT "top"
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void DeletingAFileAndMovingAnotherInLeavesTheDirectory()
        {
            Tidy.Delete("dir/file1.txt");
            Tidy.Rename("file2.txt", "dir/file2.txt", ScoreIsIrrelevant);

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("dir/file1.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Rename("file2.txt", "dir/file2.txt", ScoreIsIrrelevant));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void RemovingAllFilesRemovesAllParents()
        {
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");
            Tidy.Delete("topDir/midDir/midFile.txt");
            Tidy.Delete("topDir/topFile.txt");
            Tidy.Delete("rootFile.txt"); // This is to check that the root folder doesn't get changed.

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/midDir/midFile.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir/topFile.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("rootFile.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("topDir"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void TidyDoesNotCareWhatCaseYouUse()
        {
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/MIDFILE.TXT");
            Tidy.Delete("TOPDIR/TOPFILE.TXT");

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("TOPDIR/MIDDIR/MIDFILE.TXT"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("TOPDIR/TOPFILE.TXT"));
            Mock.Get(mockWorkspace).Verify(x => x.Delete("TOPDIR"));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void HandlesEditAndRenameOnSameFile()
        {
            Tidy.Edit("topDir/midDir/bottomDir/file1.txt");
            Tidy.Rename("topDir/midDir/bottomDir/file1.txt", "topDir/midDir/bottomDir/file1renamed.txt", ScoreIsIrrelevant);

            TidyDisposeToProcess();

            Mock.Get(mockWorkspace).Verify(x => x.Edit("topDir/midDir/bottomDir/file1.txt"));
            Mock.Get(mockWorkspace).Verify(x => x.Rename("topDir/midDir/bottomDir/file1.txt", "topDir/midDir/bottomDir/file1renamed.txt", ScoreIsIrrelevant));
            Mock.Get(mockWorkspace).VerifyNoOtherCalls();
        }

        [Fact]
        public void TidyThrowsWhenMultipleOperationsOnTheSameFileOccur()
        {
            var workspace = mocks.OneOf<ITfsWorkspaceModifier>();
            ITfsWorkspaceModifier tidy = new DirectoryTidier(workspace, Enumerable.Empty<TfsTreeEntry>);

            tidy.Delete("file.txt");
            Assert.Throws<ArgumentException>(() =>
                tidy.Add("FILE.TXT"));
            Assert.Throws<ArgumentException>(() =>
                tidy.Delete("File.TXT"));
            Assert.Throws<ArgumentException>(() =>
                tidy.Edit("File.txt"));
            Assert.Throws<ArgumentException>(() =>
                tidy.Rename("File.txt", "renamed.txt", ScoreIsIrrelevant));
            Assert.Throws<ArgumentException>(() =>
                tidy.Rename("oldFile.txt", "File.txt", ScoreIsIrrelevant));
        }

        private TfsTreeEntry item(TfsItemType itemType, string gitPath) => new TfsTreeEntry(gitPath, mocks.OneOf<IItem>().Tap(mockItem => Mock.Get(mockItem).Setup(x => x.ItemType).Returns(itemType)));

        /// <summary>
        /// The score argument is passed through by DirectoryTidier, so its value doesn't matter.
        /// </summary>
        private const string ScoreIsIrrelevant = "irrelevant";
    }
}
