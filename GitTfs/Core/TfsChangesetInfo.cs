using System;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class TfsChangesetInfo
    {
        public GitTfsRemote Remote { get; set; }
        public long ChangesetId { get; set; }
        public string GitCommit { get; set; }

        public static TfsChangesetInfo TryParse(string gitTfsMetaInfo, IGitRepository repository, string commit)
        {
            var match = GitTfsConstants.TfsCommitInfoRegex.Match(gitTfsMetaInfo);
            if (match.Success)
            {
                var commitInfo = ObjectFactory.GetInstance<TfsChangesetInfo>();
                commitInfo.Remote = repository.ReadTfsRemote(match.Groups["url"].Value, match.Groups["repository"].Value);
                commitInfo.ChangesetId = Convert.ToInt32(match.Groups["changeset"].Value);
                commitInfo.GitCommit = commit;
                return commitInfo;
            }
            return null;
        }
    }
}
