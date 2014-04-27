using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Sep.Git.Tfs.Core.TfsInterop;
using Xunit;

namespace Sep.Git.Tfs.Test.Integration
{
    public class FetchTests : IDisposable
    {
        private readonly IntegrationHelper integrationHelper;

        public FetchTests()
        {
            integrationHelper = new IntegrationHelper();
        }

        public void Dispose()
        {
            integrationHelper.Dispose();
        }

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

        [FactExceptOnUnix]
        public void FetchShouldGetAllNewChangesetsFromTfs()
        {
            var tfsUrl = "$/MyProject";

            integrationHelper.SetupGitRepo("MyProject", g => g.Commit("non tfs start"));
            integrationHelper.SetupFake(r =>
                                        {
                                            r.Changeset(1, "Changeset 1.")
                                                .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject")
                                                .Change(TfsChangeType.Add, TfsItemType.File,
                                                    "$/MyProject/file1.txt", "CS1");
                r.Changeset(2, "Changeset 2.")
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/file2.txt", "CS2");
                r.Changeset(3, "Changeset 3.")
                 .Change(TfsChangeType.Edit, TfsItemType.File, "$/MyProject/file1.txt", "CS3");
            });
            integrationHelper.RunIn("MyProject", "init", "http://server/tfs", tfsUrl);

            integrationHelper.RunIn("MyProject", "fetch");
            integrationHelper.AssertGitRepo("MyProject");

            var repo = integrationHelper.Repository("MyProject");
            var tfsRemote = repo.Lookup<Commit>("refs/remotes/tfs/default");
            var head = repo.Head.Commits.Single();

            Assert.Contains("non tfs start", head.Message);
            Assert.Equal(new [] {"Changeset 3.", "Changeset 2.", "Changeset 1."}, GetSingleParentSequence(tfsRemote).Select(x => GetFirstLine(x.Message)));
            
        }

        private IEnumerable<Commit> GetSingleParentSequence(Commit commit)
        {
            var commits = new List<Commit>();
            while (commit != null)
            {
                commits.Add(commit);
                commit = commit.Parents.SingleOrDefault();
            }
            return commits;
        }

        private string GetFirstLine(string message)
        {
            return message.Split('\r', '\n').First();
        }

        [FactExceptOnUnix]
        public void FetchShouldCheckForLatestChangesetInAllRemotes()
        {
            // This test checks that fetch will find the latest TFS changeset even
            // if it is not found upstream to HEAD. This is a common case if TFS changesets
            // are distributed between multiple git repositories. For example, if each member 
            // in a team wants to sync with TFS, but also sync their git repos, then TFS duplicates
            // will easily occur if they are git-shared and someone syncs with a HEAD behind the 
            // latest TFS sync commit.
            // This test verifies, that fetch will always find the latest changeset, even if HEAD is behind
            // or even in a different branch.
            //
            // C1-->C2
            // |
            // ---->local 1
            //
            // REFS:
            // HEAD          -> local 1
            // tfs/default   -> C1
            // origin/master -> C2

            Commit startTfsHead = null;
            Commit remoteTfsHead = null;
            var tfsUrl = "$/MyProject";
            integrationHelper.SetupGitRepo("MyProject", g =>
            {
                startTfsHead = g.Commit("Changeset 1.\n\ngit-tfs-id: [http://server/tfs]$/MyProject;C1");
                g.Ref("refs/remotes/tfs/default", startTfsHead);

                remoteTfsHead = g.Commit("Changeset 2.\n\ngit-tfs-id: [http://server/tfs]$/MyProject;C2", parentCommit: startTfsHead);
                g.Ref("refs/remotes/origin/master", remoteTfsHead);

                var localHead = g.Commit("Local commit 1.", parentCommit: startTfsHead);
                g.Ref("refs/heads/master", localHead);
            });
            integrationHelper.SetupFake(r =>
            {
                r.Changeset(1, "Changeset 1.")
                 .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject")
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README.txt, Changeset 1.");
                r.Changeset(2, "Changeset 2, but this one isn't used.")
                 .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/README.txt", "Changeset 2.");
            });
            integrationHelper.RunIn("MyProject", "init", "http://server/tfs", tfsUrl);
            integrationHelper.AssertRef("MyProject", "refs/remotes/tfs/default", startTfsHead.Sha);
            integrationHelper.RunIn("MyProject", "fetch");
            integrationHelper.AssertRef("MyProject", "refs/remotes/tfs/default", remoteTfsHead.Sha);
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

        private void AddNewCommitToFakeTfsServer()
        {
            integrationHelper.SetupFake(r => CreateAChangeset(r));
        }

        private static IntegrationHelper.FakeChangesetBuilder CreateAChangeset(IntegrationHelper.FakeHistoryBuilder r)
        {
            return r.Changeset(3, "Add a file", DateTime.Parse("2012-01-03 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Foo")
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject/Foo/Bar")
                    .Change(TfsChangeType.Add, TfsItemType.File, "$/MyProject/Foo/Bar/File.txt", "File contents");
        }
    }
}