using System;
using System.Linq;
using Xunit;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Test.Core
{
    public class RemoteConfigConverterTests
    {
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
