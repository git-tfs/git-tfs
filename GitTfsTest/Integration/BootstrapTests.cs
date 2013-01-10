using System;
using Xunit;

namespace Sep.Git.Tfs.Test.Integration
{
    public class BootstrapTests : IDisposable
    {
        IntegrationHelper h = new IntegrationHelper();

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
    }
}
