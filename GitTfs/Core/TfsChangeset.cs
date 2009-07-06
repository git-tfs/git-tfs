using System;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangeset
    {
        public string TfsUrl { get; set; }
        public string TfsSourcePath { get; set; }
        public long ChangesetId { get; set; }
        public GitTfsRemote Remote { get; set; }
        public string GitCommit { get; set; }

        public static TfsChangeset TryParse(string gitTfsMetaInfo)
        {
            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);
            if (match.Success)
            {
                var commitInfo = ObjectFactory.GetInstance<TfsChangeset>();
                commitInfo.TfsUrl = match.Groups["url"].Value;
                commitInfo.TfsSourcePath = match.Groups["repository"].Value;
                commitInfo.ChangesetId = Convert.ToInt32(match.Groups["changeset"].Value);
                return commitInfo;
            }
            return null;
        }
    }
}
