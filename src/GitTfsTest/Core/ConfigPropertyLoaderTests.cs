using GitTfs.Core;
using GitTfs.Util;

using StructureMap;

using Xunit;

namespace GitTfs.Test.Integration
{
    public class ConfigPropertyLoaderTests : BaseTest, IDisposable
    {
        private readonly IntegrationHelper h = new IntegrationHelper();

        public ConfigPropertyLoaderTests()
        {
            h.SetupFake(_ => { });
        }

        public void Dispose() => h.Dispose();

        [Fact]
        public void WhenNoValueIsSet_ThenDefaultValueIsReturned()
        {
            h.SetupGitRepo("repo", g =>
            {
                g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C1");
            });

            using (var repo = h.Repository("repo"))
            {
                var gitRepository = new GitRepository(repo.Info.WorkingDirectory, new Container(), null, new RemoteConfigConverter());
                var configProperties = new ConfigProperties(new ConfigPropertyLoader(new Globals() { Repository = gitRepository }));
                Assert.Equal(100, configProperties.BatchSize);
            }
        }

        [Fact]
        public void WhenANewValueIsSet_ThenThisValueIsReturned()
        {
            h.SetupGitRepo("repo", g =>
            {
                g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C1");
            });

            h.SetConfig("repo", GitTfsConstants.BatchSize, "25");
            using (var repo = h.Repository("repo"))
            {
                var gitRepository = new GitRepository(repo.Info.WorkingDirectory, new Container(), null, new RemoteConfigConverter());
                var configProperties = new ConfigProperties(new ConfigPropertyLoader(new Globals() { Repository = gitRepository }));

                configProperties.BatchSize = 10;
                Assert.Equal(10, configProperties.BatchSize);
            }
        }

        [Fact]
        public void WhenANewValueIsSetThrougthGitConfigFile_ThenThisValueIsReturned()
        {
            h.SetupGitRepo("repo", g =>
            {
                g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C1");
            });

            h.SetConfig("repo", GitTfsConstants.BatchSize, "5");
            using (var repo = h.Repository("repo"))
            {
                var gitRepository = new GitRepository(repo.Info.WorkingDirectory, new Container(), null, new RemoteConfigConverter());
                var configProperties = new ConfigProperties(new ConfigPropertyLoader(new Globals() { Repository = gitRepository }));

                Assert.Equal(5, configProperties.BatchSize);
            }
        }

        [Fact]
        public void WhenANewValueIsSetAndPersistedInTheGitConfigFile_ThenThisValueAppearInTheGitConfigFile()
        {
            h.SetupGitRepo("repo", g =>
            {
                g.Commit("A sample commit from TFS.\n\ngit-tfs-id: [http://server/tfs]$/MyProject/trunk;C1");
            });

            using (var repo = h.Repository("repo"))
            {
                var gitRepository = new GitRepository(repo.Info.WorkingDirectory, new Container(), null, new RemoteConfigConverter());
                var configProperties = new ConfigProperties(new ConfigPropertyLoader(new Globals() { Repository = gitRepository }));

                configProperties.BatchSize = 7;
                configProperties.PersistAllOverrides();
                Assert.Equal("7", h.GetConfig<string>("repo", GitTfsConstants.BatchSize));
            }
        }
    }
}
