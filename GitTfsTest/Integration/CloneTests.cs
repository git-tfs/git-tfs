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
            const string expectedSha = "dd806911118e6fa16d028b322ad91360d56ea47b";
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
            AssertRefs("2697fc1748a13832ef25804ef2d3be65a7cd3129");
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
            AssertRefs("4307a071b67c51bf141b34e931faa8fe166924b1");
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
            AssertRefs("fd91b4eacedb8ae519c7046f6bb60b0ce9894cb2");
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
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                r.Changeset(2, "First commit", DateTime.Parse("2012-01-02 12:12:12"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr");
                r.Changeset(2, "Second commit", DateTime.Parse("2012-01-02 12:12:12"))
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/myproject/folder/file2.txt", "File contents in lowercase path");
            });
            h.Run("clone", h.TfsUrl, "$/MyProject");
            h.AssertGitRepo("MyProject");
            h.AssertCleanWorkspace("MyProject");
        }
    }
}
