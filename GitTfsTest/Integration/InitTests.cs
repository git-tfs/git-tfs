using System;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Test.Integration
{
    public class InitTests : BaseTest, IDisposable
    {
        private readonly IntegrationHelper h;

        public InitTests()
        {
            h = new IntegrationHelper();
        }

        public void Dispose()
        {
            h.Dispose();
        }

        [FactExceptOnUnix]
        public void InitializesConfig()
        {
            h.SetupFake(r => { });
            h.Run("init", "http://my-tfs.local/tfs", "$/MyProject", "MyProject");
            h.AssertConfig("MyProject", "tfs-remote.default.url", "http://my-tfs.local/tfs");
            h.AssertConfig("MyProject", "tfs-remote.default.repository", "$/MyProject");
        }

        [FactExceptOnUnix]
        public void CanUseThatConfig()
        {
            h.SetupFake(r =>
            {
                r.Changeset(1, "Project created from template", DateTime.Parse("2012-01-01 12:12:12 -05:00"))
                    .Change(TfsChangeType.Add, TfsItemType.Folder, "$/MyProject");
            });
            h.Run("init", "http://my-tfs.local/tfs", "$/MyProject", "MyProject");
            h.SetConfig("MyProject", "tfs-remote.default.autotag", "true");
            h.RunIn("MyProject", "fetch");
            var expectedSha = "f8b247c3298f4189c6c9ff701f147af6e1428f97";
            h.AssertRef("MyProject", "refs/remotes/tfs/default", expectedSha);
            h.AssertRef("MyProject", "refs/tags/tfs/default/C1", expectedSha);
        }
    }
}
