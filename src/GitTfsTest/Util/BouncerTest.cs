using Xunit;
using GitTfs.Util;

namespace GitTfs.Test.Util
{
    public class BouncerTest : BaseTest
    {
        private readonly Bouncer bouncer = new Bouncer();

        [Fact]
        public void NoExpressionsMeansNotMatched() => Assert.False(bouncer.IsIncluded("$/Any/Path"));

        [Fact]
        public void IgnoreNullExpressions()
        {
            bouncer.Include(null);
            bouncer.Exclude(null);
            Assert.False(bouncer.IsIncluded("$/Any/Path"));
        }

        [Fact]
        public void IncludesEverything()
        {
            bouncer.Include(".*");
            Assert.True(bouncer.IsIncluded("$/Any/Path"));
        }

        [Fact]
        public void IncludesEverythingExceptSomething()
        {
            bouncer.Include(".*");
            bouncer.Exclude("something");
            Assert.True(bouncer.IsIncluded("$/Any/Path"));
            Assert.False(bouncer.IsIncluded("$/something/Path"));
        }

        [Fact]
        public void IgnoresCase()
        {
            bouncer.Include("thing");
            bouncer.Exclude("other/thing");
            Assert.True(bouncer.IsIncluded("$/Thing/Path"));
            Assert.False(bouncer.IsIncluded("$/Other/Thing/Path"));
        }

        [Fact]
        public void PrefersExclusion()
        {
            bouncer.Include(".*");
            bouncer.Exclude(".*");
            Assert.False(bouncer.IsIncluded("$/Any/Path"));
        }

        [Fact]
        public void IncludesAndExcludesAll()
        {
            bouncer.Include("\\.exe$");
            bouncer.Include("\\.dll$");
            bouncer.Exclude("/ext/");
            bouncer.Exclude("/deps/");
            Assert.True(bouncer.IsIncluded("$/Any/Path/bin/example.exe"));
            Assert.True(bouncer.IsIncluded("$/Any/Path/bin/example.dll"));
            Assert.False(bouncer.IsIncluded("$/Any/Path/ext/example.exe"));
            Assert.False(bouncer.IsIncluded("$/Any/Path/ext/example.dll"));
            Assert.False(bouncer.IsIncluded("$/Any/Path/deps/example.exe"));
            Assert.False(bouncer.IsIncluded("$/Any/Path/deps/example.dll"));
        }
    }
}
