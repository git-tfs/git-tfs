using System;
using System.Linq;
using Sep.Git.Tfs.Core.TfsInterop;
using Xunit;

namespace Sep.Git.Tfs.Test.Integration
{
    //NOTE: All timestamps in these tests must specify a time zone. If they don't, the local time zone will be used in the DateTime,
    //      but the commit timestamp will use the ToUniversalTime() version of the DateTime.
    //      This will cause the hashes to differ on computers in different time zones.
    public class CloneTests : IDisposable
    {
        IntegrationHelper h;

        public CloneTests()
        {
            h = new IntegrationHelper();
        }

        public void Dispose()
        {
            h.Dispose();
        }

        [FactExceptOnUnix(Skip="eventually")]
        public void FailOnNoProject()
        {
        }

        [FactExceptOnUnix]
        public void ClonesEmptyProject()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertEmptyWorkspace("MyProject");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "db32a67aafa0a85ae2107e3210da9611f06c1c30",
                tree: "4b825dc642cb6eb9a060e54bf8d69288fbee4904");
        }

        [FactExceptOnUnix]
        public void CloneProjectWithChangesets()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertCommitMessage("MyProject", "HEAD", "First commit", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C2");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "26fe80d7ac739585ce69cdef04191ec7572fee7b",
                tree: "41ab05d8f2a0f7f7f3a39c623e94fee68f64797e");
        }

        [FactExceptOnUnix]
        public void CloneProjectWithInternationalCharactersInFileNamesAndFolderNames()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/ÆØÅ")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/ÆØÅ/äöü.txt", "File contents");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertFileInWorkspace("MyProject", "ÆØÅ/äöü.txt", "File contents");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "337ee1fa394729ddf758c3832bc5f05f38ebc52a",
                tree: "14f207f532105e6df76cf69d6481d84b9e5b17ad");
        }

        [FactExceptOnUnix]
        public void CloneProjectWithInternationalCharactersInFileContents()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "Blåbærsyltetøy er godt!"); // "Blueberry jam is tasty!"
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "Blåbærsyltetøy er godt!");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "7ffa44247e0b3aa6272e3f15c8bdfd8c1ef94771",
                tree: "57336850a107184ca05911c9ac6cba8d1fd212fc");
        }

        [FactExceptOnUnix]
        public void CloneProjectWithInternationalCharactersInCommitMessages()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Blåbærsyltetøy", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");

            h.AssertCommitMessage("MyProject", "HEAD", "Blåbærsyltetøy", "", "git-tfs-id: [http://does/not/matter]$/MyProject;C2");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "a350378a2d054a81b641f0af8d2c294fa81ca82c",
                tree: "3f8b26f2594b7ca2370388c99739e56a64954f00");
        }

        [FactExceptOnUnix]
        public void CloneWithMixedUpCase()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Foo")
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Foo/Bar")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Foo/Bar/File.txt", "File contents");
                r.Changeset(3, "Second commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Edit, TfsItemType.File, "$/myproject/foo/BAR/file.txt", "Updated file contents in path with different casing")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/myproject/FOO/bar/file2.txt", "Another file in the same folder, but with different casing");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertCleanWorkspace("MyProject");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "10271dedb9729b1b0940113772526d149539dd8e",
                tree: "c962b51eb5397f1b98f662c9d43e6be13b7065f1");
        }

        [FactExceptOnUnix]
        public void CloneProjectWithMergeChangeset()
        {
            //History of changesets:
            //6
            //|\
            //| 5
            //| |
            //3 4
            //| /
            //2
            //|
            //1
            h.SetupFake(r =>
            {
                r.SetRootBranch("$/MyProject/Main");
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Main")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Main/File.txt", "File contents");
                r.Changeset(3, "commit in main", DateTime.Parse("2012-01-02 12:12:13 -05:00"))
                    .Change(TfsChangeType.Edit, TfsItemType.File, "$/MyProject/Main/File.txt", "File contents_main");
                r.BranchChangeset(4, "create branch", DateTime.Parse("2012-01-02 12:12:14 -05:00"), fromBranch: "$/MyProject/Main", toBranch: "$/MyProject/Branch", rootChangesetId: 2)
                    .Change(TfsChangeType.Branch, TfsItemType.Folder, "$/MyProject/Branch")
                    .Change(TfsChangeType.Branch, TfsItemType.File, "$/MyProject/Branch/File.txt", "File contents");
                r.Changeset(5, "commit in branch", DateTime.Parse("2012-01-02 12:12:15 -05:00"))
                    .Change(TfsChangeType.Edit, TfsItemType.File, "$/MyProject/Branch/File.txt", "File contents_branch");
                r.MergeChangeset(6, "merge in main", DateTime.Parse("2012-01-02 12:12:16 -05:00"), fromBranch: "$/MyProject/Branch", intoBranch: "$/MyProject/Main", lastChangesetId: 5)
                    .Change(TfsChangeType.Edit | TfsChangeType.Merge, TfsItemType.File, "$/MyProject/Main/File.txt", "File contents_main_branch=>_merge");
            });

            h.Run("clone", h.TfsUrl, "$/MyProject/Main", "MyProject", "--with-branches");

            h.AssertFileInWorkspace("MyProject", "File.txt", "File contents_main_branch=>_merge");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "e04652ded438d48f5a6c24106caf798b478a26b4",
                tree: "cf8a497b3a40028bee363a613fe156b9d37350bb");
            AssertNewClone("MyProject", new[] { "refs/heads/Branch", "refs/remotes/tfs/Branch" },
                commit: "278756339885c299f7d9312bfcda6454b57f7db9",
                tree: "c379179fee2ce45e44a5a2dd1d9bcf5ce8489608");
        }

        private readonly string[] RefsInNewClone = new[] { "HEAD", "refs/heads/master", "refs/remotes/tfs/default" };

        /// <summary>
        /// Verify repo layout.
        /// The tree verifies the correctness of the filenames and contents.
        /// The commit verifies the correctness of the commit message, author, and date, too.
        /// </summary>
        /// <param name="repodir">The repo dir.</param>
        /// <param name="refs">Refs to inspect</param>
        /// <param name="commit">(optional) The expected commit sha.</param>
        /// <param name="tree">(optional) The expected tree sha.</param>
        private void AssertNewClone(string repodir, string[] refs, string commit = null, string tree = null)
        {
            const string format = "{0}: {1} / {2}";
            var expected = String.Join("\n", refs.Select(gitref => String.Format(format, gitref, commit, tree)));
            var actual = String.Join("\n", refs.Select(gitref =>
            {
                var actualCommit = h.RevParseCommit(repodir, gitref);
                return String.Format(format, gitref,
                    commit == null || actualCommit == null ? null : actualCommit.Sha,
                    tree == null || actualCommit == null ? null : actualCommit.Tree.Sha);
            }));
            Assert.Equal(expected, actual);
        }

        #region ignore regexes

        [FactExceptOnUnix]
        public void IgnoresAFile()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Add some files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr\nanother line\n")
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/app.exe", "Do not include");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject", "--ignore-regex=.exe$");
            h.AssertNoFileInWorkspace("MyProject", "app.exe");
        }

        [FactExceptOnUnix]
        public void WorksForACommitWithOnlyIgnoredFiles()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Add some files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr\nanother line\n");
                r.Changeset(3, "Add an ignored file", DateTime.Parse("2012-01-03 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/app.exe", "Do not include");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject", "--ignore-regex=.exe$");
            h.AssertFileInWorkspace("MyProject", "README", "tldr\nanother line\n");
            h.AssertNoFileInWorkspace("MyProject", "app.exe");
        }

        [FactExceptOnUnix]
        public void HandlesIgnoredFilesParticipatingInRenames()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Add some files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", itemId: 100, contents: "tldr\nanother line\n")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/notignored.txt", itemId: 101, contents: "originalname: notignored.txt")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/ignoredatfirst.exe", itemId: 102, contents: "originalname: ignoredatfirst.exe")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/alwaysignored.exe", itemId: 103, contents: "originalname: alwaysignored.exe")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/neverignored.txt", itemId: 104, contents: "originalname: neverignored.txt");
                r.Changeset(3, "Rename the ignored files", DateTime.Parse("2012-01-03 12:12:12 -05:00"))
                    .Change(TfsChangeType.Rename, TfsItemType.File, "$/MyProject/notignored.exe", itemId: 101, contents: "originalname: notignored.txt")
                    .Change(TfsChangeType.Rename, TfsItemType.File, "$/MyProject/ignoredatfirst.txt", itemId: 102, contents: "originalname: ignoredatfirst.exe")
                    .Change(TfsChangeType.Rename, TfsItemType.File, "$/MyProject/foreverignored.exe", itemId: 103, contents: "originalname: alwaysignored.exe")
                    .Change(TfsChangeType.Rename, TfsItemType.File, "$/MyProject/included.txt", itemId: 104, contents: "originalname: neverignored.txt");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject", "--ignore-regex=.exe$");
            h.AssertTreeEntries("MyProject", "HEAD", "README", "ignoredatfirst.txt", "included.txt");
        }

        #endregion
    }
}
