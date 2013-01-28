using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Sep.Git.Tfs.Core;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Test.Core
{
    public class RemoteConfigConverterTests
    {
        public class DumpTests
        {
            RemoteConfigConverter _dumper = new RemoteConfigConverter();

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
                    Autotag = true,
                    Aliases = new string[] { "http://abc", "http://def" },
                };
                var config = _dumper.Dump(remote);
                AssertContainsConfig("tfs-remote.default.url", "http://server/path", config);
                AssertContainsConfig("tfs-remote.default.repository", "$/Project", config);
                AssertContainsConfig("tfs-remote.default.username", "user", config);
                AssertContainsConfig("tfs-remote.default.password", "pass", config);
                AssertContainsConfig("tfs-remote.default.ignore-paths", "abc", config);
                AssertContainsConfig("tfs-remote.default.legacy-urls", "http://abc,http://def", config);
                AssertContainsConfig("tfs-remote.default.autotag", "true", config);
            }

            private void AssertContainsConfig(string key, string value, IEnumerable<ConfigurationEntry> configs)
            {
                Assert.Contains(new ConfigurationEntry(key, value, ConfigurationLevel.Local), configs, configComparer);
            }

            static IEqualityComparer<ConfigurationEntry> configComparer = new ConfigurationEntryComparer();

            class ConfigurationEntryComparer : IEqualityComparer<ConfigurationEntry>
            {
                bool IEqualityComparer<ConfigurationEntry>.Equals(ConfigurationEntry x, ConfigurationEntry y)
                {
                    return x.Key == y.Key && x.Value == y.Value;
                }

                int IEqualityComparer<ConfigurationEntry>.GetHashCode(ConfigurationEntry obj)
                {
                    return obj.Key.GetHashCode();
                }
            }
        }

        public class LoadTests
        {
            RemoteConfigConverter _loader = new RemoteConfigConverter();

            private IEnumerable<RemoteInfo> Load(params ConfigurationEntry[] configs)
            {
                return _loader.Load(configs);
            }

            private ConfigurationEntry c(string key, string value)
            {
                return new ConfigurationEntry(key, value, ConfigurationLevel.Local);
            }

            [Fact]
            public void NoConfig()
            {
                var remotes = _loader.Load(Enumerable.Empty<ConfigurationEntry>());
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
                Assert.Equal(1, remotes.Count());
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
                    c("tfs-remote.default.legacy-urls", "http://old:8080/,http://other/"),
                    c("tfs-remote.default.autotag", "true"));
                Assert.Equal(1, remotes.Count());
                var remote = remotes.First();
                Assert.Equal("default", remote.Id);
                Assert.Equal("http://server/path", remote.Url);
                Assert.Equal("$/project", remote.Repository);
                Assert.Equal("theuser", remote.Username);
                Assert.Equal("thepassword", remote.Password);
                Assert.Equal("ignorethis.zip", remote.IgnoreRegex);
                Assert.Equal(new string[] { "http://old:8080/", "http://other/" }, remote.Aliases);
                Assert.True(remote.Autotag);
            }
        }

        RemoteConfigConverter _converter = new RemoteConfigConverter();

        [Fact]
        public void MultipleRemotes()
        {
            var remote1 = new RemoteInfo { Id = "a", Url = "http://a", Repository = "$/a" };
            var remote2 = new RemoteInfo { Id = "b", Url = "http://b", Repository = "$/b" };
            var config = new List<ConfigurationEntry>();
            config.AddRange(_converter.Dump(remote1));
            config.AddRange(_converter.Dump(remote2));
            var remotes = _converter.Load(config.Where(e => e.Value != null));
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

            var remotes = _converter.Load(config.Where(e => e.Value != null));
            Assert.Equal(1, remotes.Count());
            Assert.Equal("has.dots.in.it", remotes.First().Id);
        }
    }
}
