using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Test.Integration
{
    //NOTE: All timestamps in these tests must specify a time zone. If they don't, the local time zone will be used in the DateTime,
    //      but the commit timestamp will use the ToUniversalTime() version of the DateTime.
    //      This will cause the hashes to differ on computers in different time zones.
    [TestClass]
    public class CloneTests
    {
        IntegrationHelper h;

        [TestInitialize]
        public void Setup()
        {
            h = new IntegrationHelper();
        }

        [TestCleanup]
        public void Teardown()
        {
            h.Dispose();
        }

        [TestMethod, Ignore]
        public void FailOnNoProject()
        {
        }

        [TestMethod, Ignore]
        public void ClonesEmptyProject()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertGitRepo("MyProject");
            const string expectedSha = "tbd";
            h.AssertRef("MyProject", "HEAD", expectedSha);
            h.AssertRef("MyProject", "master", expectedSha);
            h.AssertRef("MyProject", "tfs/default", expectedSha);
            h.AssertEmptyWorkspace("MyProject");
        }

        [TestMethod]
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
            const string expectedSha = "72a03802ac5f864a40a9bee13608f85e0e2ad05b";
            h.AssertRef("MyProject", "HEAD", expectedSha);
            h.AssertRef("MyProject", "master", expectedSha);
            h.AssertRef("MyProject", "tfs/default", expectedSha);
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
            h.AssertFileInWorkspace("MyProject", "README", "tldr");
        }

        [TestMethod]
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
            AssertRefs("ea7ed178fb4cce7f46d2c84b907a88fa9d194014");
            h.AssertFileInWorkspace("MyProject", "ÆØÅ/äöü.txt", "File contents");
        }

        [TestMethod]
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
            AssertRefs("78f0490e22ae245a63238744de2d96f0675880a0");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "Blåbærsyltetøy er godt!");
        }

        [TestMethod]
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
            AssertRefs("9a73fe007130ca91517283aafe1d442f406df973");
            h.AssertFileInWorkspace("MyProject", "Folder/File.txt", "File contents");
        }

        private void AssertRefs(string expectedSha)
        {
            h.AssertRef("MyProject", "HEAD", expectedSha);
            h.AssertRef("MyProject", "master", expectedSha);
            h.AssertRef("MyProject", "tfs/default", expectedSha);
        }

        [TestMethod]
        public void CloneWithtMixedUpCase()
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
            AssertRefs("70cdbdca83c3808e60bc1f8cde7e155055447df7");
        }
    }
}
