using System;
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

        IEnumerable<ConfigurationEntry> _gitConfig { get { return _config.Select(x => new ConfigurationEntry(x.Key, x.Value)); } }
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
            Assert.False(_firstRemote.NoMetaData);
        }

        void SetUpCompleteRemote()
        {
            SetUpMinimalRemote();
            _config["tfs-remote.default.username"] = "theuser";
            _config["tfs-remote.default.password"] = "thepassword";
            _config["tfs-remote.default.no-meta-data"] = "true";
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
            Assert.True(_firstRemote.NoMetaData);
            Assert.Equal(new string[] { "http://old:8080/", "http://other/" }, _firstRemote.Aliases);
            Assert.True(_firstRemote.Autotag);
        }

        [Fact]
        public void NoMetaDataCanBe_1()
        {
            SetUpMinimalRemote();
            _config["tfs-remote.default.no-meta-data"] = "1";
            Assert.True(_firstRemote.NoMetaData);
        }

        [Fact]
        public void NoMetaDataCanBe_false()
        {
            SetUpMinimalRemote();
            _config["tfs-remote.default.no-meta-data"] = "false";
            Assert.False(_firstRemote.NoMetaData);
        }
    }
}
