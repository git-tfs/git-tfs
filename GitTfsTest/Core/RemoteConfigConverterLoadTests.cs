﻿using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Sep.Git.Tfs.Core;
using Xunit;

namespace Sep.Git.Tfs.Test.Core
{
    public class RemoteConfigConverterLoadTests
    {
        RemoteConfigConverter _reader = new RemoteConfigConverter();
        Dictionary<string, string> _config = new Dictionary<string, string>();

        IEnumerable<ConfigurationEntry> _gitConfig { get { return _config.Select(x => new ConfigurationEntry(x.Key, x.Value, ConfigurationLevel.Local)); } }
        IEnumerable<RemoteInfo> _remotes { get { return _reader.Load(_gitConfig); } }
        RemoteInfo _firstRemote { get { return _remotes.FirstOrDefault(); } }

        public RemoteConfigConverterLoadTests()
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

        private void SetUpMinimalRemote(string remoteId)
        {
            _config["tfs-remote." + remoteId + ".url"] = "http://server/path";
            _config["tfs-remote." + remoteId + ".repository"] = "$/project";
        }

        [Fact]
        public void MinimalRemote()
        {
            var remoteId = "default";
            SetUpMinimalRemote(remoteId);
            VerifyMinimalRemote(remoteId);
        }

        private void SetUpCompleteRemote(string remoteId)
        {
            SetUpMinimalRemote(remoteId);
            _config["tfs-remote." + remoteId + ".username"] = "theuser";
            _config["tfs-remote." + remoteId + ".password"] = "thepassword";
            _config["tfs-remote." + remoteId + ".ignore-paths"] = "ignorethis.zip";
            _config["tfs-remote." + remoteId + ".legacy-urls"] = "http://old:8080/,http://other/";
            _config["tfs-remote." + remoteId + ".autotag"] = "true";
        }

        [Fact]
        public void CompleteRemote()
        {
            var remoteId = "default";
            SetUpCompleteRemote(remoteId);
            VerifyCompleteRemote(remoteId);
        }

        [Fact]
        public void MinimalRemoteWithDotInName()
        {
            var remoteId = "maint-1.0.0.0";
            SetUpCompleteRemote(remoteId);
            VerifyCompleteRemote(remoteId);
        }

        [Fact]
        public void CompleteRemoteWithDotInName()
        {
            var remoteId = "maint-1.0.0.0";
            SetUpCompleteRemote(remoteId);
            VerifyCompleteRemote(remoteId);
        }

        private void VerifyCompleteRemote(string remoteId)
        {
            Assert.Equal(remoteId, _firstRemote.Id);
            Assert.Equal("http://server/path", _firstRemote.Url);
            Assert.Equal("$/project", _firstRemote.Repository);
            Assert.Equal("theuser", _firstRemote.Username);
            Assert.Equal("thepassword", _firstRemote.Password);
            Assert.Equal("ignorethis.zip", _firstRemote.IgnoreRegex);
            Assert.Equal(new string[] { "http://old:8080/", "http://other/" }, _firstRemote.Aliases);
            Assert.True(_firstRemote.Autotag);
        }

        private void VerifyMinimalRemote(string remoteId)
        {
            Assert.Equal(1, _remotes.Count());
            Assert.Equal(remoteId, _firstRemote.Id);
            Assert.Equal("http://server/path", _firstRemote.Url);
            Assert.Equal("$/project", _firstRemote.Repository);
            Assert.Null(_firstRemote.Username);
            Assert.Null(_firstRemote.Password);
            Assert.Null(_firstRemote.IgnoreRegex);
        }
    }
}
