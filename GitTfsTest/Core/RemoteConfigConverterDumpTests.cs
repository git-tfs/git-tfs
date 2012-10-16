using System;
using System.Collections.Generic;
using LibGit2Sharp;
using Xunit;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Core
{
    public class RemoteConfigConverterDumpTests
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
            AssertContainsConfig("tfs-remote.default.no-meta-data", null, config);
            AssertContainsConfig("tfs-remote.default.ignore-paths", null, config);
            AssertContainsConfig("tfs-remote.default.legacy-urls", null, config);
            AssertContainsConfig("tfs-remote.default.autotag", null, config);
        }

        [Fact]
        public void DumpsCompleteRemote()
        {
            var remote = new RemoteInfo {
                Id = "default",
                Url = "http://server/path",
                Repository = "$/Project",
                Username = "user",
                Password = "pass",
                IgnoreRegex = "abc",
                NoMetaData = true,
                Autotag = true,
                Aliases = new string[] { "http://abc", "http://def" },
            };
            var config = _dumper.Dump(remote);
            AssertContainsConfig("tfs-remote.default.url", "http://server/path", config);
            AssertContainsConfig("tfs-remote.default.repository", "$/Project", config);
            AssertContainsConfig("tfs-remote.default.username", "user", config);
            AssertContainsConfig("tfs-remote.default.password", "pass", config);
            AssertContainsConfig("tfs-remote.default.no-meta-data", "true", config);
            AssertContainsConfig("tfs-remote.default.ignore-paths", "abc", config);
            AssertContainsConfig("tfs-remote.default.legacy-urls", "http://abc,http://def", config);
            AssertContainsConfig("tfs-remote.default.autotag", "true", config);
        }

        private void AssertContainsConfig(string key, string value, IEnumerable<ConfigurationEntry> configs)
        {
            Assert.Contains(new ConfigurationEntry(key, value), configs, comparer);
        }

        static IEqualityComparer<ConfigurationEntry> comparer = new ConfigurationEntryComparer();

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
}
