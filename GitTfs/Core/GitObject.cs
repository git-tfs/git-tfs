namespace Sep.Git.Tfs.Core
{
    public class GitObject
    {
        public string Mode { get; set; }
        public string ObjectType { get; set; }
        public string Sha { get; set; }
        public string Commit { get; set; }
        public string Path { get; set; }
    }
}