using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using GitTfs.Core.TfsInterop;
using GitTfs.Test.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GitTfs.Test.Integration
{
    //NOTE: All timestamps in these tests must specify a time zone. If they don't, the local time zone will be used in the DateTime,
    //      but the commit timestamp will use the ToUniversalTime() version of the DateTime.
    //      This will cause the hashes to differ on computers in different time zones.
    public class CloneTests : BaseTest, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IntegrationHelper h;

        public CloneTests(ITestOutputHelper output)
        {
            _output = output;
            h = new IntegrationHelper();
            _output.WriteLine("Repository in folder: " + h.Workdir);
        }

        public void Dispose()
        {
            h.Dispose();
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
                commit: "4053764b2868a2be71ae7f5f113ad84dff8a052a",
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
                commit: "d64d883266eca65bede947c79529318718a0d8eb",
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
                commit: "4faa9a5f32e6af118b84071a537228d3f7da7d9d",
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
                commit: "5bd7660fa145ce0c38b5c279502478ce205a0cfb",
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
                commit: "cd14e6e28abffd625412dae36d9d9659bf6cb68c",
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
                commit: "175420603e41cd0175e3c25581754726bd21cb96",
                tree: "c962b51eb5397f1b98f662c9d43e6be13b7065f1");
        }

        private void CreateFakeRepositoryWithMergeChangeset()
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
        }

        [FactExceptOnUnix]
        public void WhenCloningTrunkWithMergeChangesetWithAllBranches_ThenThe2BranchesAreAutomaticallyInitialized()
        {
            CreateFakeRepositoryWithMergeChangeset();

            h.Run("clone", h.TfsUrl, "$/MyProject/Main", "MyProject", "--branches=all");

            h.AssertFileInWorkspace("MyProject", "File.txt", "File contents_main_branch=>_merge");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "be59d37d08a0cc78916f04a256dc52f6722f800c",
                tree: "cf8a497b3a40028bee363a613fe156b9d37350bb");
            AssertNewClone("MyProject", new[] { "refs/heads/Branch", "refs/remotes/tfs/Branch" },
                commit: "0df2815a74403cfe96ccb96e3f995970f55df2b4",
                tree: "c379179fee2ce45e44a5a2dd1d9bcf5ce8489608");
        }

        [FactExceptOnUnix]
        public void WhenCloningTrunkWithMergeChangeset_ThenTheMergedBranchIsAutomaticallyInitialized()
        {
            CreateFakeRepositoryWithMergeChangeset();

            h.Run("clone", h.TfsUrl, "$/MyProject/Main", "MyProject");

            h.AssertFileInWorkspace("MyProject", "File.txt", "File contents_main_branch=>_merge");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "be59d37d08a0cc78916f04a256dc52f6722f800c",
                tree: "cf8a497b3a40028bee363a613fe156b9d37350bb");
            AssertNewClone("MyProject", new[] { "refs/remotes/tfs/Branch" },
                commit: "0df2815a74403cfe96ccb96e3f995970f55df2b4",
                tree: "c379179fee2ce45e44a5a2dd1d9bcf5ce8489608");
        }

        [FactExceptOnUnix]
        public void WhenCloningFunctionalTestVtccdsWithBranchesRenaming_ThenAllRenamesShouldBeWellHandled()
        {
            h.SetupFake(r =>
            {
                r.SetRootBranch("$/vtccds/trunk");
                vtccds.Prepare(r);
            });
            h.TfsUrl = "https://tfs.codeplex.com:443/tfs/TFS16";
            h.Run("clone", h.TfsUrl, "$/vtccds/trunk", "Vtccds", "--branches=all");

            AssertNewClone("Vtccds", new[] { "refs/heads/master", "refs/remotes/tfs/default" }, commit: "e7d54b14fbdcbbc184d58e82931b7c1ac4a2be70");
            AssertNewClone("Vtccds", new[] { "refs/heads/b1", "refs/remotes/tfs/b1" }, commit: "3cdb2a311ac7cbda1e892a9b3371a76c871a696a");
            AssertNewClone("Vtccds", new[] { "refs/heads/b1.1", "refs/remotes/tfs/b1.1" }, commit: "e6e79221fd35b2002367a41535de9c43b626150a");
            AssertNewClone("Vtccds", new[] { "refs/heads/renameFile", "refs/remotes/tfs/renameFile" }, commit: "003ca02adfd9561418f05a61c7a999386957a146");
            AssertNewClone("Vtccds", new[] { "refs/remotes/tfs/branch_from_nowhere" }, commit: "9cb91c60d76d00af182ae9f16da6e6aa77b88a5e");
            AssertNewClone("Vtccds", new[] { "refs/heads/renamed3", "refs/remotes/tfs/renamed3" }, commit: "615ac5588d3cb6282c2c7d514f2828ad3aeaf5c7");

            //No refs for renamed branches
            h.AssertNoRef("Vtccds", "refs/remotes/tfs/renamedTwice");
            h.AssertNoRef("Vtccds", "refs/remotes/tfs/afterRename");
            h.AssertNoRef("Vtccds", "refs/remotes/tfs/testRename");
        }

        [FactExceptOnUnix]
        public void WhenCloningFunctionalTestVtccdsWithBranchesRenamingAndGitignore_ThenAllRenamesShouldBeWellHandled()
        {
            // This test duplicates WhenCloningFunctionalTestVtccdsWithBranchesRenaming_ThenAllRenamesShouldBeWellHandled, but with
            // a .gitignore file included via the --gitignore option. It was added so that we have coverage of the complex renaming
            // scenarios in Vtccds both with and without use of the --gitignore option.
            //
            // The .gitignore included in this variant of the Vtccds test is the first commit of the repository and is the ancestral
            // parent of all the branches in this repository except for the refs/remotes/tfs/branch_from_nowhere. It is not expected
            // to be in the refs/remotes/tfs/branch_from_nowhere branch, because that branch came from a TFS changeset that is not
            // rooted at the base of the $/vtccds/trunk TFS project.

            string gitignoreFile = Path.Combine(h.Workdir, "gitignore");
            string gitignoreContent = "*.exe\r\n*.com\r\n";
            File.WriteAllText(gitignoreFile, gitignoreContent);

            h.SetupFake(r =>
            {
                r.SetRootBranch("$/vtccds/trunk");
                vtccds.Prepare(r);
            });
            h.TfsUrl = "https://tfs.codeplex.com:443/tfs/TFS16";
            h.Run("clone", h.TfsUrl, "$/vtccds/trunk", "Vtccds", "--branches=all", $"--gitignore={gitignoreFile}");

            // The commit hashes for all of the refs below (except for refs/remotes/tfs/branch_from_nowhere - see above) reflect the fact
            // that the first commit to the repository is the .gitignore specified with the --gitignore option. As a result, these hashes
            // differ from those of the WhenCloningFunctionalTestVtccdsWithBranchesRenaming_ThenAllRenamesShouldBeWellHandled test.
            AssertNewClone("Vtccds", new[] { "refs/heads/master", "refs/remotes/tfs/default" }, commit: "2c047e8c26998bd261ec50716e8574e691efc990");
            AssertNewClone("Vtccds", new[] { "refs/heads/b1", "refs/remotes/tfs/b1" }, commit: "5afc5abed88f15087796e8419aee6c133b8f1786");
            AssertNewClone("Vtccds", new[] { "refs/heads/b1.1", "refs/remotes/tfs/b1.1" }, commit: "cc1cbc9fd62fa7e4ee55a3619858b27c62eb9c00");
            AssertNewClone("Vtccds", new[] { "refs/heads/renameFile", "refs/remotes/tfs/renameFile" }, commit: "cd9aafa3bae84c2b55def73c6dc1cad0dc83dd76");
            AssertNewClone("Vtccds", new[] { "refs/remotes/tfs/branch_from_nowhere" }, commit: "9cb91c60d76d00af182ae9f16da6e6aa77b88a5e");
            AssertNewClone("Vtccds", new[] { "refs/heads/renamed3", "refs/remotes/tfs/renamed3" }, commit: "891e14ba4977835cb1ca06f1ceaa5c7948ea5785");

            //No refs for renamed branches
            h.AssertNoRef("Vtccds", "refs/remotes/tfs/renamedTwice");
            h.AssertNoRef("Vtccds", "refs/remotes/tfs/afterRename");
            h.AssertNoRef("Vtccds", "refs/remotes/tfs/testRename");
        }

        [FactExceptOnUnix]
        public void WhenCloningTrunkWithIgnoringBranches_ThenTheMergedBranchIsAutomaticallyInitialized()
        {
            CreateFakeRepositoryWithMergeChangeset();

            h.Run("clone", h.TfsUrl, "$/MyProject/Main", "MyProject", "--branches=none");

            h.AssertFileInWorkspace("MyProject", "File.txt", "File contents_main_branch=>_merge");
            AssertNewClone("MyProject", RefsInNewClone,
                commit: "843a915aea98894fef51379d68a0f309e8537dd5",
                tree: "cf8a497b3a40028bee363a613fe156b9d37350bb");
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
            var expected = string.Join("\n", refs.Select(gitref => string.Format(format, gitref, commit, tree)));
            var actual = string.Join("\n", refs.Select(gitref =>
            {
                var actualCommit = h.RevParseCommit(repodir, gitref);
                return string.Format(format, gitref,
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
        public void LineEndingsNormalizedWhenAutocrlf()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Add some files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tld \r\n another line \r\n");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject","--autocrlf=true");
            
            h.AssertFileInWorkspace("MyProject", "README", "tld \r\n another line \r\n");
            h.AssertFileInIndex("MyProject", "README", "tld \n another line \n");
        }
        
        [FactExceptOnUnix]
        public void LineNotNormalizedWhenAutocrlfFalse()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Add some files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tld \r\n another line \r\n");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject", "--autocrlf=false");
            h.AssertFileInWorkspace("MyProject", "README", "tld \r\n another line \r\n");
            h.AssertFileInIndex("MyProject", "README", "tld \r\n another line \r\n");
        }

        [FactExceptOnUnix]
        public void LineNotNormalizedWhenGitIgnoreGivenAndAutocrlfFalse_Issue1398()
        {
            string readmeContent = "tld \r\n another line \r\n";
            string gitignoreFile = Path.Combine(h.Workdir, "gitignore");
            string gitignoreContent = "*.exe\r\n*.com\r\n";
            File.WriteAllText(gitignoreFile, gitignoreContent);

            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Add some files", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", readmeContent);
            });

            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject", "--autocrlf=false", $"--gitignore={gitignoreFile}");

            // The file given in --gitignore parameter is imported as .gitignore, no line ending conversion
            h.AssertFileInWorkspace("MyProject", ".gitignore", gitignoreContent);
            h.AssertFileInIndex("MyProject", ".gitignore", gitignoreContent);

            // README is imported as is, no line ending conversion
            h.AssertFileInWorkspace("MyProject", "README", readmeContent);
            h.AssertFileInIndex("MyProject", "README", readmeContent);
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

        [FactExceptOnUnix]
        public void CloneWithAllBranchesShouldHandleFolderDeletedAndRecreatedAsBranch()
        {
            h.SetupFake(r =>
            {
                r.SetRootBranch("$/MyTeamProject/Root");

                r.Changeset(1, "Create initial team project.", DateTime.Now)
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyTeamProject");

                r.Changeset(2, "Create root branch.", DateTime.Now)
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyTeamProject/Root");

                r.Changeset(3, @"Create ""branch"" (as a folder).", DateTime.Now)
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyTeamProject/Branch");

                r.Changeset(4, @"Delete the ""branch"" folder.", DateTime.Now)
                    .Change(TfsChangeType.Delete, TfsItemType.Folder, "$/MyTeamProject/Branch");

                r.BranchChangeset(5, "Create a proper branch (though, with the same name as the previously deleted folder)",
                        DateTime.Now, "$/MyTeamProject/Root", "$/MyTeamProject/Branch", 2)
                    .Change(TfsChangeType.Branch, TfsItemType.Folder, "$/MyTeamProject/Branch");
            });

            h.Run("clone", h.TfsUrl, "$/MyTeamProject/Root", "MyTeamProject", "--branches=all");

            h.AssertGitRepo("MyTeamProject");

            var branchCollection = h.Repository("MyTeamProject").Branches.Cast<Branch>().ToList();
            var branch = branchCollection.FirstOrDefault(b => b.FriendlyName == "Branch");
            Assert.NotNull(branch);

            // So, it turns out GetRootChangesetForBranch is really the unit under test here.
            // Because it's faked out for unit tests, this test is worthless except as an
            // illustration of expected behavior in the actual implementation.

            // Ensure we didn't migrate our branch from the "folder creation" point of
            // $/MyTeamProject/Branch (e.g. C2 -> C3, C4...
            Assert.DoesNotContain(branch.Commits, c => c.Message.IndexOf(@"Create ""branch"" (as a folder).", StringComparison.InvariantCultureIgnoreCase) >= 0);

            // Ensure we migrated the branch from it's creation point, e.g. C2 immediately followed by C5 (C2 -> C5)
            var expectedBranchChangesetParentCommit = branch.Commits.Where(c => c.Message.IndexOf("Create root branch.", StringComparison.InvariantCultureIgnoreCase) >= 0).FirstOrDefault();
            Assert.NotNull(expectedBranchChangesetParentCommit);

            var branchChangesetCommit = branch.Commits.Where(c => c.Message.IndexOf("Create a proper branch (though, with the same name as the previously deleted folder)", StringComparison.InvariantCultureIgnoreCase) >= 0).FirstOrDefault();
            Assert.NotNull(branchChangesetCommit);

            // This wasn't part of the original test by @jeremy-sylvis-tmg, but this does ensure the relationship
            // C2 -> C5; it may not be enough to just ensure C5 exists.
            Assert.Contains(expectedBranchChangesetParentCommit, branchChangesetCommit.Parents);

            var refs = new[]
            {
                "HEAD",
                "refs/remotes/tfs/default",
                "refs/heads/master",
                "refs/heads/tfs/branch",
                "refs/heads/Branch"
            };
            AssertNewClone("MyTeamProject", refs);
        }

        [FactExceptOnUnix]
        public void CloneUsingInitialBranchOption()
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
            h.Run("clone", h.TfsUrl, "$/MyProject", "--initial-branch=customInitialBranch");
            h.AssertCommitMessage("MyProject", "HEAD", "First commit", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C2");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
            AssertNewClone("MyProject", new[] { "HEAD", "refs/heads/customInitialBranch", "refs/remotes/tfs/default" },
                commit: "d64d883266eca65bede947c79529318718a0d8eb",
                tree: "41ab05d8f2a0f7f7f3a39c623e94fee68f64797e");
        }

        [FactExceptOnUnix]
        public void CloneWithMainAndGitignore()
        {
            // Verifies that no extraneous "master" branch is introduced when git is configured to use
            // a default initial branch that is not "master"

            string gitignoreFile = Path.Combine(h.Workdir, "gitignore");
            string gitignoreContent = "*.exe\r\n*.com\r\n";
            File.WriteAllText(gitignoreFile, gitignoreContent);

            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr");
            });
            h.RunInWithConfig(".", "GitTfs.Test.Integration.GlobalConfigs.mainDefaultBranch.gitconfig", "clone", h.TfsUrl, "$/MyProject", "MyProject", $"--gitignore={gitignoreFile}");
            h.AssertCommitMessage("MyProject", "HEAD", "First commit", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C2");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
            AssertNewClone("MyProject", new[] { "HEAD", "refs/heads/main", "refs/remotes/tfs/default" },
                commit: "1e9f1c2dfc1a0b5e4e6f135525a60d7c33a2d0aa",
                tree: "2ef92a065910b3cc3a1379e41a034e90f2e610ec");
            h.AssertNoRef("MyProject", "master");
        }

        [FactExceptOnUnix]
        public void CloneWithFirstTFSChangesetIsRename()
        {
            // Tests for the special case where the first TFS changeset is a rename changeset and --gitignore is not used (see issue #1409)

            h.SetupFake(r =>
            {
                r.Changeset(1, "First TFS changeset: rename the top-level folder", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Rename, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Second TFS changeset: one folder and two files added", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject");
            h.AssertCommitMessage("MyProject", "HEAD", "Second TFS changeset: one folder and two files added", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C2");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
            AssertNewClone("MyProject", new[] { "HEAD", "refs/heads/master", "refs/remotes/tfs/default" },
                commit: "726e937beab54f17fae545744497d68aa7c36507",
                tree: "41ab05d8f2a0f7f7f3a39c623e94fee68f64797e");
        }

        [FactExceptOnUnix]
        public void CloneWithFirstTFSChangesetIsRenameAndGitignoreGiven()
        {
            // Tests for the special case where the first TFS changeset is a rename changeset and --gitignore is used (see issue #1409)

            string gitignoreFile = Path.Combine(h.Workdir, "gitignore");
            string gitignoreContent = "*.exe\r\n*.com\r\n";
            File.WriteAllText(gitignoreFile, gitignoreContent);

            h.SetupFake(r =>
            {
                r.Changeset(1, "First TFS changeset: rename the top-level folder", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Rename, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "Second TFS changeset: one folder and two files added", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject", "MyProject", $"--gitignore={gitignoreFile}");
            h.AssertCommitMessage("MyProject", "HEAD", "Second TFS changeset: one folder and two files added", "", "git-tfs-id: [" + h.TfsUrl + "]$/MyProject;C2");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
            h.AssertFileInWorkspace("MyProject", ".gitignore", gitignoreContent);
            AssertNewClone("MyProject", new[] { "HEAD", "refs/heads/master", "refs/remotes/tfs/default" },
                commit: "d1802bd0cee53f20ed69f182d1835e93697762a1",
                tree: "2ef92a065910b3cc3a1379e41a034e90f2e610ec");
        }
    }
}
