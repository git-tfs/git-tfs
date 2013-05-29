using System;
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
            h.AssertGitRepo("MyProject");
            const string expectedSha = "4053764b2868a2be71ae7f5f113ad84dff8a052a";
            h.AssertRef("MyProject", "HEAD", expectedSha);
            h.AssertRef("MyProject", "master", expectedSha);
            h.AssertRef("MyProject", "refs/remotes/tfs/default", expectedSha);
            h.AssertEmptyWorkspace("MyProject");
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
            h.AssertGitRepo("MyProject");
            const string expectedSha = "d64d883266eca65bede947c79529318718a0d8eb";
            h.AssertRef("MyProject", "HEAD", expectedSha);
            h.AssertRef("MyProject", "master", expectedSha);
            h.AssertRef("MyProject", "refs/remotes/tfs/default", expectedSha);
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
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
            h.AssertGitRepo("MyProject");
            AssertRefs("4faa9a5f32e6af118b84071a537228d3f7da7d9d");
            h.AssertFileInWorkspace("MyProject", "ÆØÅ/äöü.txt", "File contents");
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
            h.AssertGitRepo("MyProject");
            AssertRefs("5bd7660fa145ce0c38b5c279502478ce205a0cfb");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "Blåbærsyltetøy er godt!");
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
            h.AssertGitRepo("MyProject");
            AssertRefs("cd14e6e28abffd625412dae36d9d9659bf6cb68c");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");

            var expectedCommitMessage = new System.Text.StringBuilder();
            expectedCommitMessage.AppendLine("Blåbærsyltetøy");
            expectedCommitMessage.AppendLine("");
            expectedCommitMessage.AppendLine("git-tfs-id: [http://does/not/matter]$/MyProject;C2");

            h.AssertCommitMessage("MyProject", "HEAD", expectedCommitMessage.ToString());
        }

        private void AssertRefs(string expectedSha)
        {
            h.AssertRef("MyProject", "HEAD", expectedSha);
            h.AssertRef("MyProject", "master", expectedSha);
            h.AssertRef("MyProject", "refs/remotes/tfs/default", expectedSha);
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
            h.AssertGitRepo("MyProject");
            h.AssertCleanWorkspace("MyProject");
            AssertRefs("175420603e41cd0175e3c25581754726bd21cb96");
        }

        [FactExceptOnUnix]
        public void CloneWithTrickyRenameAndDelete()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/File1.txt", "File1")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/File2.txt", "File2", itemId: 1000);
                r.Changeset(3, "Second commit", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                    .Change(TfsChangeType.Delete, TfsItemType.File, "$/MyProject/File1.txt")
                    .Change(TfsChangeType.Rename, TfsItemType.File, "$/MyProject/File1.txt", "File2", itemId: 1000);
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertGitRepo("MyProject");
            h.AssertFileInWorkspace("MyProject", "File1.txt", "File2");
            h.AssertNoFileInWorkspace("MyProject", "File2.txt");
        }
    }
}
