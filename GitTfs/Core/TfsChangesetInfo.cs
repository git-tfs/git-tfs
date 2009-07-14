using System;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo
    {
        public string TfsUrl { get; set; }
        public string TfsSourcePath { get; set; }
        public DateTime TfsCheckinDate { get; set; }
        public long ChangesetId { get; set; }
        public GitTfsRemote Remote { get; set; }
        public string GitCommit { get; set; }

        public static TfsChangesetInfo TryParse(string gitTfsMetaInfo)
        {
            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);
            if (match.Success)
            {
                var commitInfo = ObjectFactory.GetInstance<TfsChangesetInfo>();
                commitInfo.TfsUrl = match.Groups["url"].Value;
                commitInfo.TfsSourcePath = match.Groups["repository"].Value;
                commitInfo.ChangesetId = Convert.ToInt32(match.Groups["changeset"].Value);
                return commitInfo;
            }
            return null;
        }
    }
}
