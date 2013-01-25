using System;
using System.Collections.Generic;
using Xunit;
using Rhino.Mocks;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Test.Core
{
    public class DirectoryTidierTests : IDisposable
    {
        MockRepository mocks;
        ITfsWorkspaceModifier mockWorkspace;
        TfsTreeEntry [] initialTfsTree;
        DirectoryTidier _tidy;

        public DirectoryTidierTests()
        {
            mocks = new MockRepository();
            mockWorkspace = mocks.StrictMock<ITfsWorkspaceModifier>();
            initialTfsTree = new TfsTreeEntry [] {
                item(TfsItemType.Folder, "topDir"),
                item(TfsItemType.File,   "topDir/topFile.txt"),
                item(TfsItemType.Folder, "topDir/midDir"),
                item(TfsItemType.File,   "topDir/midDir/midFile.txt"),
                item(TfsItemType.Folder, "topDir/midDir/bottomDir"),
                item(TfsItemType.File,   "topDir/midDir/bottomDir/file1.txt"),
                item(TfsItemType.File,   "topDir/midDir/bottomDir/file2.txt"),
                item(TfsItemType.Folder, "dir1"),
                item(TfsItemType.Folder, "dir1/dir2"),
                item(TfsItemType.Folder, "dir1/dir2/dir3"),
                item(TfsItemType.File,   "dir1/dir2/dir3/lonelyFile.txt"),
                item(TfsItemType.File,   "rootFile.txt"),
            };
        }

        public void Dispose()
        {
            Tidy.Dispose();
            mockWorkspace.VerifyAllExpectations();
        }

        DirectoryTidier Tidy
        {
            get
            {
                if (_tidy == null)
                {
                    mocks.ReplayAll();
                    _tidy = new DirectoryTidier(mockWorkspace, initialTfsTree);
                }
                return _tidy;
            }
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
            Tidy.Dispose();
            Tidy.Dispose();
        }

        [Fact]
        public void RemovingAFileRemovesAllEmptyParents()
        {
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3/lonelyFile.txt"));
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3"));
            mockWorkspace.Expect(x => x.Delete("dir1/dir2"));
            mockWorkspace.Expect(x => x.Delete("dir1"));
            Tidy.Delete("dir1/dir2/dir3/lonelyFile.txt");
        }

        [Fact]
        public void MovingAFileOutRemovesAllEmptyParents()
        {
            mockWorkspace.Expect(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3"));
            mockWorkspace.Expect(x => x.Delete("dir1/dir2"));
            mockWorkspace.Expect(x => x.Delete("dir1"));
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);
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
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir"));
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
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR"));
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/MIDFILE.TXT");
            Tidy.Delete("TOPDIR/TOPFILE.TXT");
        }

        TfsTreeEntry item(TfsItemType itemType, string gitPath)
        {
            return new TfsTreeEntry(gitPath, mocks.StrictMock<IItem>().Tap(mockItem => mockItem.Stub(x => x.ItemType).Return(itemType)));
        }

        /// <summary>
        /// The score argument is passed through by DirectoryTidier, so its value doesn't matter.
        /// </summary>
        const string ScoreIsIrrelevant = "irrelevant";
    }
}
