using System;
using System.IO;
using GitTfs.Core.TfsInterop;
using Xunit.Abstractions;

namespace GitTfs.Test.Integration
{
    public class InitTests : BaseTest, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IntegrationHelper h;

        public InitTests(ITestOutputHelper output)
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

        [FactExceptOnUnix]
        public void InitializesConfigUsingNoParallel()
        {
            h.SetupFake(r => { });
            h.Run("init", "http://my-tfs.local/tfs", "$/MyProject", "MyProject", "--no-parallel");
            h.AssertConfig("MyProject", "tfs-remote.default.noparallel", "true");
        }

        [FactExceptOnUnix]
        public void InitializesWithInitialBranchArg()
        {
            h.SetupFake(r => { });
            h.Run("init", "http://my-tfs.local/tfs", "$/MyProject", "MyProject", "--initial-branch=customInitialBranch");
            h.AssertHead("MyProject", "refs/heads/customInitialBranch");
        }

        [FactExceptOnUnix]
        public void InitializesWithGitignore()
        {
            // Tests both:
            //   1. No extraneous "master" branch is introduced when gitconfig calls for "main" as the initial branch
            //   2. The initial .gitignore commit is on both the main branch and the tfs remote so they have common history

            string gitignoreFile = Path.Combine(h.Workdir, "gitignore");
            string gitignoreContent = "*.exe\r\n*.com\r\n";
            File.WriteAllText(gitignoreFile, gitignoreContent);

            h.SetupFake(r => { });
            h.RunInWithConfig(".", "GitTfs.Test.Integration.GlobalConfigs.mainDefaultBranch.gitconfig", "init", "http://my-tfs.local/tfs", "$/MyProject", "MyProject", $"--gitignore={gitignoreFile}");
            h.AssertNoRef("MyProject", "refs/heads/master");
            h.AssertRef("MyProject", "refs/heads/main", "077fd68c084ef718a505f0a7375330c68d699f40");
            h.AssertRef("MyProject", "refs/remotes/tfs/default", "077fd68c084ef718a505f0a7375330c68d699f40");
        }
    }
}
