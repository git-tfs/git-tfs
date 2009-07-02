using System.Text.RegularExpressions;

namespace Sep.Git.Tfs
{
    public static class GitTfsConstants
    {
        public static readonly Regex Sha1 = new Regex("[a-f\\d]{40}",   RegexOptions.IgnoreCase);
        public static readonly Regex Sha1Short = new Regex("[a-f\\d]{4,40}", RegexOptions.IgnoreCase);
        public const string DefaultRepositoryId = "default";
    }
}
