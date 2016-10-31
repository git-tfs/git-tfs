using System;
using System.Linq;
using Xunit;
using Rhino.Mocks;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Test.Core
{
    public class DirectoryTidierTests : BaseTest, IDisposable
    {
        private readonly MockRepository mocks;
        private readonly ITfsWorkspaceModifier mockWorkspace;
        private readonly TfsTreeEntry[] initialTfsTree;
        private DirectoryTidier _tidy;

        public DirectoryTidierTests()
        {
            mocks = new MockRepository();
            mockWorkspace = mocks.StrictMock<ITfsWorkspaceModifier>();
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

        public void Dispose()
        {
            TidyDispose();
            mockWorkspace.VerifyAllExpectations();
        }

        private ITfsWorkspaceModifier Tidy
        {
            get
            {
                if (_tidy == null)
                {
                    mocks.ReplayAll();
                    _tidy = new DirectoryTidier(mockWorkspace, () => initialTfsTree);
                }
                return _tidy;
            }
        }

        private void TidyDispose()
        {
            ((IDisposable)Tidy).Dispose();
        }

        [Fact]
        public void PassesThroughGetLocalPath()
        {
            mockWorkspace.Expect(x => x.GetLocalPath("git-path")).Return("tfs-path");
            Assert.Equal("tfs-path", Tidy.GetLocalPath("git-path"));
        }

        [Fact]
        public void NoChangesMeansNoChanges()
        {
            // nothing!
        }

        [Fact]
        public void AddingAFilePassesThroughAndDoesNotRemoveOtherItems()
        {
            mockWorkspace.Expect(x => x.Add("topDir/midDir/bottomDir/newFile.txt"));
            Tidy.Add("topDir/midDir/bottomDir/newFile.txt");
        }

        [Fact]
        public void RemovingAFileWithSiblingsDoesNotRemoveTheDir()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
        }

        [Fact]
        public void RemovingBothSiblingFilesRemovesTheDir()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");
        }

        [Fact]
        public void NoDoubleDelete()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");
            TidyDispose();
            TidyDispose();
        }

        [Fact]
        public void RemovingAFileRemovesAllEmptyParents()
        {
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3/lonelyFile.txt"));
            mockWorkspace.Expect(x => x.Delete("dir1"));
            Tidy.Delete("dir1/dir2/dir3/lonelyFile.txt");
        }


        [Fact]
        public void SourceDirectoryOfRenameShouldNotBeDeleted()
        {
            // Even though a directory may end up empty after a file has been moved from it,
            // TFS does not allow deleting that empty directory.
            // See https://github.com/git-tfs/git-tfs/issues/313

            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Rename("topDir/midDir/bottomDir/file2.txt", "file2.txt", ScoreIsIrrelevant));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Rename("topDir/midDir/bottomDir/file2.txt", "file2.txt", ScoreIsIrrelevant);
            // "topDir/midDir/bottomDir/" becomes empty but is not deleted
        }

        [Fact]
        public void MovingAFileOutLeavesAllEmptyParents()
        {
            mockWorkspace.Expect(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);
        }

        [Fact]
        public void DeleteOnlyTheTopOfEmptyDirTree()
        {
            mockWorkspace.Expect(x => x.Delete("dirA/file.txt"));
            mockWorkspace.Expect(x => x.Delete("dirA/dirB/file.txt"));
            mockWorkspace.Expect(x => x.Delete("dirA"));
            Tidy.Delete("dirA/file.txt");
            Tidy.Delete("dirA/dirB/file.txt");
        }

        [Fact]
        public void MovingAFileOutAndInLeavesParents()
        {
            mockWorkspace.Expect(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            mockWorkspace.Expect(x => x.Rename("topDir/topFile.txt", "dir1/dir2/dir3/replacement.txt", ScoreIsIrrelevant));
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);
            Tidy.Rename("topDir/topFile.txt", "dir1/dir2/dir3/replacement.txt", ScoreIsIrrelevant);
        }

        [Fact]
        public void DeletingAFileAndAddingAnotherLeavesParents()
        {
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3/lonelyFile.txt"));
            mockWorkspace.Expect(x => x.Add("dir1/dir2/dir3/newFile.txt"));
            Tidy.Delete("dir1/dir2/dir3/lonelyFile.txt");
            Tidy.Add("dir1/dir2/dir3/newFile.txt");
        }

        [Fact]
        public void RemovingAllFilesRemovesAllParents()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/midFile.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/topFile.txt"));
            mockWorkspace.Expect(x => x.Delete("rootFile.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");
            Tidy.Delete("topDir/midDir/midFile.txt");
            Tidy.Delete("topDir/topFile.txt");
            Tidy.Delete("rootFile.txt"); // This is to check that the root folder doesn't get changed.
        }

        [Fact]
        public void TidyDoesNotCareWhatCaseYouUse()
        {
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/MIDFILE.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/TOPFILE.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR"));
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/MIDFILE.TXT");
            Tidy.Delete("TOPDIR/TOPFILE.TXT");
        }

        [Fact]
        public void HandlesEditAndRenameOnSameFile()
        {
            mockWorkspace.Expect(x => x.Edit("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Rename("topDir/midDir/bottomDir/file1.txt", "topDir/midDir/bottomDir/file1renamed.txt", ScoreIsIrrelevant));
            Tidy.Edit("topDir/midDir/bottomDir/file1.txt");
            Tidy.Rename("topDir/midDir/bottomDir/file1.txt", "topDir/midDir/bottomDir/file1renamed.txt", ScoreIsIrrelevant);
        }

        [Fact]
        public void TidyThrowsWhenMultipleOperationsOnTheSameFileOccur()
        {
            var workspace = mocks.StrictMock<ITfsWorkspaceModifier>();
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

        private TfsTreeEntry item(TfsItemType itemType, string gitPath)
        {
            return new TfsTreeEntry(gitPath, mocks.StrictMock<IItem>().Tap(mockItem => mockItem.Stub(x => x.ItemType).Return(itemType)));
        }

        /// <summary>
        /// The score argument is passed through by DirectoryTidier, so its value doesn't matter.
        /// </summary>
        private const string ScoreIsIrrelevant = "irrelevant";
    }
}
