using GitTfs.Commands;
using GitTfs.Core;

using LibGit2Sharp;

using Xunit;

namespace GitTfs.Test.Core
{
    public class RemoteConfigConverterTests : BaseTest
    {
        public class DumpTests : BaseTest
        {
            private readonly RemoteConfigConverter _dumper = new RemoteConfigConverter();

            [Fact]
            public void DumpsNothingWithNoId()
            {
                var remote = new RemoteInfo { Url = "http://server/path", Repository = "$/Project" };
                Assert.Empty(_dumper.Dump(remote));
            }

            [Fact]
            public void DumpsNothingWithBlankId()
            {
                var remote = new RemoteInfo { Id = "  ", Url = "http://server/path", Repository = "$/Project" };
                Assert.Empty(_dumper.Dump(remote));
            }

            [Fact]
            public void DumpsMinimalRemote()
            {
                var remote = new RemoteInfo { Id = "default", Url = "http://server/path", Repository = "$/Project" };
                var config = _dumper.Dump(remote);
                AssertContainsConfig("tfs-remote.default.url", "http://server/path", config);
                AssertContainsConfig("tfs-remote.default.repository", "$/Project", config);
                AssertContainsConfig("tfs-remote.default.username", null, config);
                AssertContainsConfig("tfs-remote.default.password", null, config);
                AssertContainsConfig("tfs-remote.default.ignore-paths", null, config);
                AssertContainsConfig("tfs-remote.default.legacy-urls", null, config);
                AssertContainsConfig("tfs-remote.default.autotag", null, config);
                AssertContainsConfig("tfs-remote.default.noparallel", null, config);
            }

            [Fact]
            public void DumpsCompleteRemote()
            {
                var remote = new RemoteInfo
                {
                    Id = "default",
                    Url = "http://server/path",
                    Repository = "$/Project",
                    Username = "user",
                    Password = "pass",
                    IgnoreRegex = "abc",
                    IgnoreExceptRegex = "def",
                    GitIgnorePath = ".gitignore",
                    Autotag = true,
                    Aliases = new string[] { "http://abc", "http://def" },
                    NoParallel = true,
                };
                var config = _dumper.Dump(remote);
                AssertContainsConfig("tfs-remote.default.url", "http://server/path", config);
                AssertContainsConfig("tfs-remote.default.repository", "$/Project", config);
                AssertContainsConfig("tfs-remote.default.username", "user", config);
                AssertContainsConfig("tfs-remote.default.password", "pass", config);
                AssertContainsConfig("tfs-remote.default.ignore-paths", "abc", config);
                AssertContainsConfig("tfs-remote.default.ignore-except", "def", config);
                AssertContainsConfig("tfs-remote.default.gitignore-path", ".gitignore", config);
                AssertContainsConfig("tfs-remote.default.legacy-urls", "http://abc,http://def", config);
                AssertContainsConfig("tfs-remote.default.autotag", "true", config);
                AssertContainsConfig("tfs-remote.default.noparallel", "true", config);
            }

            /// <summary>
            /// Test to ensure that RemoteInfo properties are set correctly when populated via a RemoteOptions object.
            /// </summary>
            [Fact]
            public void DumpsCompleteRemoteAlt()
            {
                var remote = new RemoteInfo
                {
                    Id = "default",
                    Url = "http://server/path",
                    Repository = "$/Project",
                    RemoteOptions = new RemoteOptions
                    {
                        Username = "user",
                        Password = "pass",
                        IgnoreRegex = "abc",
                        ExceptRegex = "def",
                        GitIgnorePath = ".gitignore",
                        NoParallel = true
                    },
                    Autotag = true,
                    Aliases = new[] { "http://abc", "http://def" },
                };
                var config = _dumper.Dump(remote);
                AssertContainsConfig("tfs-remote.default.url", "http://server/path", config);
                AssertContainsConfig("tfs-remote.default.repository", "$/Project", config);
                AssertContainsConfig("tfs-remote.default.username", "user", config);
                AssertContainsConfig("tfs-remote.default.password", "pass", config);
                AssertContainsConfig("tfs-remote.default.ignore-paths", "abc", config);
                AssertContainsConfig("tfs-remote.default.ignore-except", "def", config);
                AssertContainsConfig("tfs-remote.default.gitignore-path", ".gitignore", config);
                AssertContainsConfig("tfs-remote.default.legacy-urls", "http://abc,http://def", config);
                AssertContainsConfig("tfs-remote.default.autotag", "true", config);
                AssertContainsConfig("tfs-remote.default.noparallel", "true", config);
            }

            /// <summary>
            /// Test to ensure that when the RemoteOptions object is retrieved from a RemoteInfo object it's properties are set correctly
            /// </summary>
            [Fact]
            public void RetrieveRemoteOptionsFromRemoteInfo()
            {
                var remote = new RemoteInfo
                {
                    Id = "default",
                    Url = "http://server/path",
                    Repository = "$/Project",
                    Username = "user",
                    Password = "pass",
                    IgnoreRegex = "abc",
                    IgnoreExceptRegex = "def",
                    GitIgnorePath = ".gitignore",
                    Autotag = true,
                    Aliases = new[] { "http://abc", "http://def" },
                    NoParallel = true
                };
                var remoteOptions = remote.RemoteOptions;

                Assert.Equal("user", remoteOptions.Username);
                Assert.Equal("pass", remoteOptions.Password);
                Assert.Equal("abc", remoteOptions.IgnoreRegex);
                Assert.Equal("def", remoteOptions.ExceptRegex);
                Assert.Equal(".gitignore", remoteOptions.GitIgnorePath);
                Assert.True(remoteOptions.NoParallel);
            }

            private void AssertContainsConfig(string key, string value, IEnumerable<KeyValuePair<string, string>> configs) => Assert.Contains(new KeyValuePair<string, string>(key, value), configs);
        }

        public class LoadTests : BaseTest
        {
            private readonly RemoteConfigConverter _loader = new RemoteConfigConverter();

            private IEnumerable<RemoteInfo> Load(params ConfigurationEntry<string>[] configs) => _loader.Load(configs);

            [Fact]
            public void NoConfig()
            {
                var remotes = _loader.Load(Enumerable.Empty<ConfigurationEntry<string>>());
                Assert.Empty(remotes);
            }

            [Fact]
            public void OnlyGitConfig()
            {
                var remotes = Load(
                    c("core.autocrlf", "true"),
                    c("ui.color", "true"));
                Assert.Empty(remotes);
            }

            [Fact]
            public void MinimalRemote()
            {
                var remotes = Load(
                    c("tfs-remote.default.url", "http://server/path"),
                    c("tfs-remote.default.repository", "$/project"));
                Assert.Single(remotes);
                var remote = remotes.First();
                Assert.Equal("default", remote.Id);
                Assert.Equal("http://server/path", remote.Url);
                Assert.Equal("$/project", remote.Repository);
                Assert.Null(remote.Username);
                Assert.Null(remote.Password);
                Assert.Null(remote.IgnoreRegex);
            }

            [Fact]
            public void RemoteWithEverything()
            {
                var remotes = Load(
                    c("tfs-remote.default.url", "http://server/path"),
                    c("tfs-remote.default.repository", "$/project"),
                    c("tfs-remote.default.username", "theuser"),
                    c("tfs-remote.default.password", "thepassword"),
                    c("tfs-remote.default.ignore-paths", "ignorethis.zip"),
                    c("tfs-remote.default.ignore-except", "dontignorethis.zip"),
                    c("tfs-remote.default.gitignore-path", ".gitignore"),
                    c("tfs-remote.default.legacy-urls", "http://old:8080/,http://other/"),
                    c("tfs-remote.default.autotag", "true"),
                    c("tfs-remote.default.noparallel", "true"));
                Assert.Single(remotes);
                var remote = remotes.First();
                Assert.Equal("default", remote.Id);
                Assert.Equal("http://server/path", remote.Url);
                Assert.Equal("$/project", remote.Repository);
                Assert.Equal("theuser", remote.Username);
                Assert.Equal("thepassword", remote.Password);
                Assert.Equal("ignorethis.zip", remote.IgnoreRegex);
                Assert.Equal("dontignorethis.zip", remote.IgnoreExceptRegex);
                Assert.Equal(".gitignore", remote.GitIgnorePath);
                Assert.Equal(new string[] { "http://old:8080/", "http://other/" }, remote.Aliases);
                Assert.True(remote.Autotag);
                Assert.True(remote.NoParallel);
            }


            [Fact]
            public void ShouldNotReturnLackingTfsUrlRemote()
            {
                var remotes = Load(
                    c("tfs-remote.default.repository", "$/project"));
                Assert.Empty(remotes);
            }
        }

        private readonly RemoteConfigConverter _converter = new RemoteConfigConverter();

        [Fact]
        public void MultipleRemotes()
        {
            var remote1 = new RemoteInfo { Id = "a", Url = "http://a", Repository = "$/a" };
            var remote2 = new RemoteInfo { Id = "b", Url = "http://b", Repository = "$/b" };
            var config = new List<KeyValuePair<string, string>>();
            config.AddRange(_converter.Dump(remote1));
            config.AddRange(_converter.Dump(remote2));
            var remotes = _converter.Load(magic(config));
            Assert.Equal(2, remotes.Count());
            Assert.Equal(new string[] { "a", "b" }, remotes.Select(r => r.Id).OrderBy(s => s));
        }

        [Fact]
        public void HandlesDotsInName()
        {
            var originalRemote = new RemoteInfo { Id = "has.dots.in.it", Url = "http://do/not/care", Repository = "$/do/not/care" };

            var config = _converter.Dump(originalRemote);
            foreach (var entry in config)
                Assert.True(entry.Key.StartsWith("tfs-remote.has.dots.in.it."), entry.Key + " should start with tfs-remote.has.dots.in.it");

            var remotes = _converter.Load(magic(config));
            Assert.Single(remotes);
            Assert.Equal("has.dots.in.it", remotes.First().Id);
        }

        private static IEnumerable<ConfigurationEntry<string>> magic(IEnumerable<KeyValuePair<string, string>> dumped) => dumped.Where(e => e.Value != null).Select(e => c(e.Key, e.Value));

        private static ConfigurationEntry<string> c(string key, string value) => new TestConfigurationEntry(key, value);

        private class TestConfigurationEntry : ConfigurationEntry<string>
        {
            private readonly string _key;
            private readonly string _value;

            public override string Key => _key;
            public override string Value => _value;

            public TestConfigurationEntry(string key, string value)
            {
                _key = key;
                _value = value;
            }
        }
    }
}
