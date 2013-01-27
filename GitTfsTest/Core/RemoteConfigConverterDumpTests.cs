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
            var remoteId = "default";
            var remote = new RemoteInfo { Id = remoteId, Url = "http://server/path", Repository = "$/Project" };
            var config = _dumper.Dump(remote);
            VerifyMinimalRemote(remoteId, config);
        }

        [Fact]
        public void DumpsCompleteRemote()
        {
            var remoteId = "default";
            var remote = new RemoteInfo {
                Id = remoteId,
                Url = "http://server/path",
                Repository = "$/Project",
                Username = "user",
                Password = "pass",
                IgnoreRegex = "abc",
                Autotag = true,
                Aliases = new string[] { "http://abc", "http://def" },
            };
            var config = _dumper.Dump(remote);
            VerifyCompleteRemote(remoteId, config);
        }

        [Fact]
        public void DumpsMinimalRemoteWithDotInName()
        {
            var remoteId = "maint-1.0.0.0";
            var remote = new RemoteInfo { Id = remoteId, Url = "http://server/path", Repository = "$/Project" };
            var config = _dumper.Dump(remote);
            VerifyMinimalRemote(remoteId, config);
        }

        [Fact]
        public void DumpsCompleteRemoteWithDotInName()
        {
            var remoteId = "maint-1.0.0.0";
            var remote = new RemoteInfo
            {
                Id = remoteId,
                Url = "http://server/path",
                Repository = "$/Project",
                Username = "user",
                Password = "pass",
                IgnoreRegex = "abc",
                Autotag = true,
                Aliases = new string[] { "http://abc", "http://def" },
            };
            var config = _dumper.Dump(remote);
            VerifyCompleteRemote(remoteId, config);
        }

        private void VerifyMinimalRemote(string remoteId, IEnumerable<ConfigurationEntry> config)
        {
            AssertContainsConfig("tfs-remote." + remoteId  + ".url", "http://server/path", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".repository", "$/Project", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".username", null, config);
            AssertContainsConfig("tfs-remote." + remoteId + ".password", null, config);
            AssertContainsConfig("tfs-remote." + remoteId + ".ignore-paths", null, config);
            AssertContainsConfig("tfs-remote." + remoteId + ".legacy-urls", null, config);
            AssertContainsConfig("tfs-remote." + remoteId + ".autotag", null, config);
        }

        private void VerifyCompleteRemote(string remoteId, IEnumerable<ConfigurationEntry> config)
        {
            AssertContainsConfig("tfs-remote." + remoteId + ".url", "http://server/path", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".repository", "$/Project", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".username", "user", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".password", "pass", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".ignore-paths", "abc", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".legacy-urls", "http://abc,http://def", config);
            AssertContainsConfig("tfs-remote." + remoteId + ".autotag", "true", config);
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
}
