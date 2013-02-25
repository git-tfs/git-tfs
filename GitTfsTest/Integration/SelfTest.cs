using System;
using Xunit;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.VsFake;

namespace Sep.Git.Tfs.Test.Integration
{
    public class SelfTest
    {
        [Fact]
        public void GetsItemContent()
        {
            var change = new ScriptedChange();
            change.Content = "The content";
            var changeset = new ScriptedChangeset();
            changeset.Changes.Add(change);
            var script = new Script();
            script.Changesets.Add(changeset);
            var tfs = new TfsHelper(null, null, script);
            Assert.Equal("The content", tfs.GetContent(changeset, change));
        }

        [Fact]
        public void GetsRenamedRenamedItemContent()
        {
            var change1 = new ScriptedChange();
            change1.ChangeType = TfsChangeType.Add;
            change1.Content = "The content";
            change1.RepositoryPath = "$/file1.txt";
            var changeset1 = new ScriptedChangeset() { Id = 1 };
            changeset1.Changes.Add(change1);

            var change2 = new ScriptedChange();
            change2.ChangeType = TfsChangeType.Rename;
            change2.RepositoryPath = "$/file2.txt";
            change2.RenamedFrom = "$/file1.txt";
            var changeset2 = new ScriptedChangeset() { Id = 2 };
            changeset2.Changes.Add(change2);

            var changeset3 = new ScriptedChangeset() { Id = 3 };

            var change4 = new ScriptedChange();
            change4.ChangeType = TfsChangeType.Rename;
            change4.RepositoryPath = "$/file4.txt";
            change4.RenamedFrom = "$/file2.txt";
            var changeset4 = new ScriptedChangeset() { Id = 4 };
            changeset4.Changes.Add(change4);

            var script = new Script();
            script.Changesets.AddRange(new [] { changeset1, changeset2, changeset3, changeset4 });

            var tfs = new TfsHelper(null, null, script);
            Assert.Equal("The content", tfs.GetContent(changeset4, change4));
        }
    }
}
