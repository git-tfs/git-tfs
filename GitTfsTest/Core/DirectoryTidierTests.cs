using System;
using Xunit;
using Rhino.Mocks;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Core
{
    [Trait("focus", "true")]
    public class DirectoryTidierTests : IDisposable
    {
        MockRepository mocks;
        ITfsWorkspaceModifier mockWorkspace;
        object initialTfsTree;
        DirectoryTidier _tidy;

        public DirectoryTidierTests()
        {
            mocks = new MockRepository();
            mockWorkspace = mocks.StrictMock<ITfsWorkspaceModifier>();
            initialTfsTree = null; // todo
            // topDir/
            // topDir/topFile.txt
            // topDir/midDir/midFile.txt
            // topDir/midDir/bottomDir/file1.txt
            // topDir/midDir/bottomDir/file2.txt
            // dir1/dir2/dir3/lonelyFile.txt
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
            Tidy.Dispose();
        }

        [Fact]
        public void AddingAFilePassesThroughAndDoesNotRemoveOtherItems()
        {
            mockWorkspace.Expect(x => x.Add("topDir/midDir/bottomDir/newFile.txt"));
            Tidy.Add("topDir/midDir/bottomDir/newFile.txt");
            Tidy.Dispose();
        }

        [Fact]
        public void RemovingAFileWithSiblingsDoesNotRemoveTheDir()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Dispose();
        }

        [Fact]
        public void RemovingBothSiblingFilesRemovesTheDir()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");
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
            Tidy.Dispose();
        }

        [Fact]
        public void MovingAFileOutRemovesAllEmptyParents()
        {
            mockWorkspace.Expect(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3"));
            mockWorkspace.Expect(x => x.Delete("dir1/dir2"));
            mockWorkspace.Expect(x => x.Delete("dir1"));
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);
            Tidy.Dispose();
        }

        [Fact]
        public void MovingAFileOutAndInLeavesParents()
        {
            mockWorkspace.Expect(x => x.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant));
            mockWorkspace.Expect(x => x.Rename("topDir/topFile.txt", "dir1/dir2/dir3/replacement.txt", ScoreIsIrrelevant));
            Tidy.Rename("dir1/dir2/dir3/lonelyFile.txt", "otherdir/otherdir2/newName.txt", ScoreIsIrrelevant);
            Tidy.Rename("topDir/topFile.txt", "dir1/dir2/dir3/replacement.txt", ScoreIsIrrelevant);
            Tidy.Dispose();
        }

        [Fact]
        public void DeletingAFileAndAddingAnotherLeavesParents()
        {
            mockWorkspace.Expect(x => x.Delete("dir1/dir2/dir3/lonelyFile.txt"));
            mockWorkspace.Expect(x => x.Add("dir1/dir2/dir3/newFile.txt"));
            Tidy.Delete("dir1/dir2/dir3/lonelyFile.txt");
            Tidy.Add("dir1/dir2/dir3/newFile.txt");
            Tidy.Dispose();
        }

        [Fact]
        public void RemovingAllFilesRemovesAllParents()
        {
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file1.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir/file2.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/midFile.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/topFile.txt"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir"));
            mockWorkspace.Expect(x => x.Delete("topDir"));
            Tidy.Delete("topDir/midDir/bottomDir/file1.txt");
            Tidy.Delete("topDir/midDir/bottomDir/file2.txt");
            Tidy.Delete("topDir/midDir/midFile.txt");
            Tidy.Delete("topDir/topFile.txt");
            Tidy.Dispose();
        }

        [Fact]
        public void TidyDoesNotCareWhatCaseYouUse()
        {
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/MIDDIR/MIDFILE.TXT"));
            mockWorkspace.Expect(x => x.Delete("TOPDIR/TOPFILE.TXT"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir/bottomDir"));
            mockWorkspace.Expect(x => x.Delete("topDir/midDir"));
            mockWorkspace.Expect(x => x.Delete("topDir"));
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE1.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/BOTTOMDIR/FILE2.TXT");
            Tidy.Delete("TOPDIR/MIDDIR/MIDFILE.TXT");
            Tidy.Delete("TOPDIR/TOPFILE.TXT");
            Tidy.Dispose();
        }

        /// <summary>
        /// The score argument is passed through by DirectoryTidier, so its value doesn't matter.
        /// </summary>
        const string ScoreIsIrrelevant = "irrelevant";
    }
}
