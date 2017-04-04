using System;
using Sep.Git.Tfs.Core.TfsInterop;
using Xunit;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Test.Integration
{
    public class BootstrapTests : BaseTest, IDisposable
    {
        private readonly IntegrationHelper h = new IntegrationHelper();

        public BootstrapTests()
        {
            h.SetupFake(_ => { });
        }

        public void Dispose()
        {
            h.Dispose();
        }

        [Fact]
        public void BootstrapWithNoRemotes()
        {
            h.SetupGitRepo("repo", g =>
            {
                g.Commit("A sample commit.");
            });
            h.RunIn("repo", "bootstrap");
            h.AssertNoRef("repo", "tfs/default");
        }

        [Fact]
        public void BootstrapWithARemoteAtHead()
        {
            string c1 = null;
            h.SetupGitRepo("repo", g =>
            {
                c1 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject;C1");
            });
            h.RunIn("repo", "bootstrap");
            h.AssertRef("repo", "tfs/default", c1);
        }

        [Fact]
        public void BootstrapWithARemoteAsAParentOfHead()
        {
            string c1 = null;
            h.SetupGitRepo("repo", g =>
            {
                c1 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject;C1");
                g.Commit("Another sample commit.");
            });
            h.RunIn("repo", "bootstrap");
            h.AssertRef("repo", "tfs/default", c1);
        }

        [Fact]
        public void WhenUsingIOption_ThenAutoBootstrapingMaster()
        {
            int ChangesetIdToTrickFetch = 1;
            h.SetupFake(r =>
            {
                r.Changeset(ChangesetIdToTrickFetch, "UseLess! Just to have the same changeset Id that the commit already in repo (and fetch nothing)", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
            });

            string c1 = null;
            h.SetupGitRepo("repo", g =>
            {
                c1 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C" + ChangesetIdToTrickFetch);
            });
            h.AssertNoRef("repo", "tfs/default");
            h.RunIn("repo", "fetch");
            h.AssertRef("repo", "tfs/default", c1);
        }

        [Fact]
        public void WhenUsingIOption_ThenAutoBootstrapingOneBrancheInAdditionToMaster()
        {
            int ChangesetIdToTrickFetch = 1;
            h.SetupFake(r =>
            {
                r.Changeset(ChangesetIdToTrickFetch, "UseLess! Just to have the same changeset Id that the commit already in repo (and fetch nothing)", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
            });

            string c1 = null;
            string c2 = null;
            h.SetupGitRepo("repo", g =>
            {
                c1 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C" + ChangesetIdToTrickFetch);
                g.CreateBranch("branch");
                g.Checkout("branch");
                c2 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/branch;C" + ChangesetIdToTrickFetch);
            });

            using (var repo = h.Repository("repo"))
            {
                LibGit2Sharp.Commands.Checkout(repo, "master");
                h.AssertNoRef("repo", "tfs/default");
                h.RunIn("repo", "fetch");
                h.AssertRef("repo", "tfs/default", c1);

                LibGit2Sharp.Commands.Checkout(repo, "branch");
                h.AssertNoRef("repo", "tfs/branch");
                h.RunIn("repo", "fetch");
                h.AssertRef("repo", "tfs/branch", c2);
            }
        }

        [Fact]
        public void WhenUsingIOption_ThenAutoBootstrapingAMergeCommit()
        {
            int ChangesetIdToTrickFetch = 1;
            h.SetupFake(r =>
            {
                r.Changeset(ChangesetIdToTrickFetch, "UseLess! Just to have the same changeset Id that the commit already in repo (and fetch nothing)", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                 .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
            });

            string c1 = null;
            string c2 = null;
            string c3 = null;
            h.SetupGitRepo("repo", g =>
            {
                c1 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C" + ChangesetIdToTrickFetch);
                g.CreateBranch("branch");
                g.Checkout("branch");
                c2 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/branch;C" + ChangesetIdToTrickFetch);
                g.Checkout("master");
                c3 = g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C" + ChangesetIdToTrickFetch);
                g.Merge("branch");
            });

            using (var repo = h.Repository("repo"))
            {
                h.AssertNoRef("repo", "tfs/default");
                h.RunIn("repo", "fetch");
                h.AssertRef("repo", "tfs/default", c3);

                h.AssertNoRef("repo", "tfs/branch");
            }
        }
    }
}
