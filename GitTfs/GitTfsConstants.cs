using System.Text.RegularExpressions;
using System;

namespace Sep.Git.Tfs
{
    public static class GitTfsConstants
    {
        public static readonly Regex Sha1 = new Regex("[a-f\\d]{40}", RegexOptions.IgnoreCase);
        public static readonly Regex Sha1Short = new Regex("[a-f\\d]{4,40}", RegexOptions.IgnoreCase);
        public static readonly Regex CommitRegex = new Regex("^commit (" + Sha1 + ")\\s*$");

        public const string DefaultRepositoryId = "default";

        public const string GitTfsPrefix = "git-tfs";
        // e.g. git-tfs-id: [http://team:8080/]$/sandbox;C123
        public const string TfsCommitInfoFormat = "git-tfs-id: [{0}]{1};C{2}";
        public static readonly Regex TfsCommitInfoRegex =
                new Regex("^\\s*" +
                          GitTfsPrefix +
                          "-id:\\s+" +
                          "\\[(?<url>.+)\\]" +
                          "(?<repository>.+)?;" +
                          "C(?<changeset>\\d+)" +
                          "\\s*$", RegexOptions.Multiline);
        // e.g. git-tfs-work-item: 24 associate
        public static readonly Regex TfsWorkItemRegex =
                new Regex(GitTfsPrefix + @"-work-item:\s*(?<item_id>\d+)\s*(?<action>associate|resolve)");

        // e.g. git-tfs-code-reviewer: John Smith
        public static readonly Regex TfsReviewerRegex =
                new Regex(GitTfsPrefix + @"-(?<type>code|security|performance)-reviewer:\s*(?<reviewer>.+)");

        // e.g. git-tfs-force: override reason
        public static readonly Regex TfsForceRegex =
                new Regex(GitTfsPrefix + @"-force:\s*(?<reason>.+)\s*$");

        /// <summary>
        /// Applied to a remote ID to determine if it is a subtree
        /// </summary>
        public static readonly Regex RemoteSubtreeRegex =
                new Regex("(?<owner>[^/]+)_subtree/(?<prefix>.+)");

        /// <summary>
        /// The format for remote IDs that are sutbrees,
        /// where {0} is the owning remote and {1} is the prefix
        /// </summary>
        public const string RemoteSubtreeFormat = "{0}_subtree/{1}";
    }
}
