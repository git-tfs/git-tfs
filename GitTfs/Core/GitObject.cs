
namespace Sep.Git.Tfs.Core
{
    public class GitObject
    {
        public LibGit2Sharp.Mode Mode { get; set; }
        public LibGit2Sharp.TreeEntryTargetType ObjectType { get; set; }
        public string Sha { get; set; }
        public string Commit { get; set; }
        public string Path { get; set; }

        public GitObject()
        {
            Mode = LibGit2Sharp.Mode.NonExecutableFile;
        }
    }
}