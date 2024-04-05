using GitTfs.Core.TfsInterop;

using Xunit;
using Xunit.Abstractions;

namespace GitTfs.Test.Integration
{
    public class FetchTests : BaseTest, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IntegrationHelper integrationHelper;

        public FetchTests(ITestOutputHelper output)
        {
            _output = output;
            integrationHelper = new IntegrationHelper();
            _output.WriteLine("Repository in folder: " + integrationHelper.Workdir);
        }

        public void Dispose() => integrationHelper.Dispose();

        [FactExceptOnUnix]
        public void CanFetchWithMixedUpCasingForTfsServerUrl()
        {
            CloneRepoWithTwoCommits();
            AddNewCommitToFakeTfsServer();
            string tfsUrlInUpperCase = integrationHelper.TfsUrl.ToUpper();
            integrationHelper.ChangeConfigSetting("MyProject", "tfs-remote.default.url", tfsUrlInUpperCase);

            integrationHelper.RunIn("MyProject", "pull");

            Assert.Equal(3, integrationHelper.GetCommitCount("MyProject"));
        }

        [FactExceptOnUnix]
        public void CanFetchWithMixedUpCasingForLegacyTfsServerUrl()
        {
            CloneRepoWithTwoCommits();
            AddNewCommitToFakeTfsServer();
            string tfsUrlInUpperCase = integrationHelper.TfsUrl.ToUpper();
            integrationHelper.ChangeConfigSetting("MyProject", "tfs-remote.default.url", "nomatch");
            integrationHelper.ChangeConfigSetting("MyProject", "tfs-remote.default.legacy-urls", tfsUrlInUpperCase + ",aDifferentUrl");

            integrationHelper.RunIn("MyProject", "pull");

            Assert.Equal(3, integrationHelper.GetCommitCount("MyProject"));
        }

        [FactExceptOnUnix]
        public void CanFetchWithMixedUpCasingForTfsRepositoryPath()
        {
            CloneRepoWithTwoCommits();
            AddNewCommitToFakeTfsServer();
            const string repoUrlInUpperCase = "$/MYPROJECT";
            integrationHelper.ChangeConfigSetting("MyProject", "tfs-remote.default.repository", repoUrlInUpperCase);

            integrationHelper.RunIn("MyProject", "pull");

            Assert.Equal(3, integrationHelper.GetCommitCount("MyProject"));
        }

        private void CloneRepoWithTwoCommits()
        {
            integrationHelper.SetupFake(r =>
                                            {
                                                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                                                 .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
                                                r.Changeset(2, "Add Readme", DateTime.Parse("2012-01-02 12:12:12 -05:00"))
                                                 .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Folder")
                                                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Folder/File.txt", "File contents")
                                                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README", "tldr");
                                            });
            integrationHelper.Run("clone", integrationHelper.TfsUrl, "$/MyProject");
            integrationHelper.AssertGitRepo("MyProject");
        }

        private void AddNewCommitToFakeTfsServer() => integrationHelper.SetupFake(r => CreateAChangeset(r));

        private static IntegrationHelper.FakeChangesetBuilder CreateAChangeset(IntegrationHelper.FakeHistoryBuilder r) => r.Changeset(3, "Add a file", DateTime.Parse("2012-01-03 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Foo")
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Foo/Bar")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Foo/Bar/File.txt", "File contents");
    }
}