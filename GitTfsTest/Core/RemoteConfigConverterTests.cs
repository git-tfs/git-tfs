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
            RemoteConfigConverter _reader = new RemoteConfigConverter();
            Dictionary<string, string> _config = new Dictionary<string, string>();

            IEnumerable<ConfigurationEntry> _gitConfig { get { return _config.Select(x => new ConfigurationEntry(x.Key, x.Value, ConfigurationLevel.Local)); } }
            IEnumerable<RemoteInfo> _remotes { get { return _reader.Load(_gitConfig); } }
            RemoteInfo _firstRemote { get { return _remotes.FirstOrDefault(); } }

            public LoadTests()
            {
                // Set some normal-ish config params. This makes sure that there is no barfing on extra config entries.
                _config["core.autocrlf"] = "true";
                _config["ui.color"] = "auto";
            }

            [Fact]
            public void NoConfig()
            {
                Assert.Empty(_remotes);
            }

            void SetUpMinimalRemote()
            {
                _config["tfs-remote.default.url"] = "http://server/path";
                _config["tfs-remote.default.repository"] = "$/project";
            }

            [Fact]
            public void MinimalRemote()
            {
                SetUpMinimalRemote();
                Assert.Equal(1, _remotes.Count());
                Assert.Equal("default", _firstRemote.Id);
                Assert.Equal("http://server/path", _firstRemote.Url);
                Assert.Equal("$/project", _firstRemote.Repository);
                Assert.Null(_firstRemote.Username);
                Assert.Null(_firstRemote.Password);
                Assert.Null(_firstRemote.IgnoreRegex);
            }

            void SetUpCompleteRemote()
            {
                SetUpMinimalRemote();
                _config["tfs-remote.default.username"] = "theuser";
                _config["tfs-remote.default.password"] = "thepassword";
                _config["tfs-remote.default.ignore-paths"] = "ignorethis.zip";
                _config["tfs-remote.default.legacy-urls"] = "http://old:8080/,http://other/";
                _config["tfs-remote.default.autotag"] = "true";
            }

            [Fact]
            public void RemoteWithEverything()
            {
                SetUpCompleteRemote();
                Assert.Equal("default", _firstRemote.Id);
                Assert.Equal("http://server/path", _firstRemote.Url);
                Assert.Equal("$/project", _firstRemote.Repository);
                Assert.Equal("theuser", _firstRemote.Username);
                Assert.Equal("thepassword", _firstRemote.Password);
                Assert.Equal("ignorethis.zip", _firstRemote.IgnoreRegex);
                Assert.Equal(new string[] { "http://old:8080/", "http://other/" }, _firstRemote.Aliases);
                Assert.True(_firstRemote.Autotag);
            }
        }

        
        [Fact]
        public void HandlesDotsInName()
        {
            var originalRemote = new RemoteInfo { Id = "has.dots.in.it", Url = "http://do/not/care", Repository = "$/do/not/care" };
            var converter = new RemoteConfigConverter();
            var config = converter.Dump(originalRemote);
            foreach (var entry in config)
                Assert.True(entry.Key.StartsWith("tfs-remote.has.dots.in.it."), entry.Key + " should start with tfs-remote.has.dots.in.it");
            var remotes = converter.Load(config.Where(e => e.Value != null));
            Assert.Equal(1, remotes.Count());
            Assert.Equal("has.dots.in.it", remotes.First().Id);
        }
    }
}
