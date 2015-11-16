using System.IO;
using Sep.Git.Tfs.Commands; // ToGitRefName() and RemoteOptions

namespace Sep.Git.Tfs.Core
{
    public class Bootstrapper
    {
        readonly Globals _globals;
        readonly TextWriter _stdout;
        readonly RemoteOptions _remoteOptions;

        public Bootstrapper(Globals globals, TextWriter stdout, RemoteOptions remoteOptions)
        {
            _globals = globals;
            _stdout = stdout;
            _remoteOptions = remoteOptions;
        }

        public IGitTfsRemote CreateRemote(TfsChangesetInfo changeset)
        {
            IGitTfsRemote remote;
            if (changeset.Remote.IsDerived)
            {
                var remoteId = GetRemoteId(changeset);
                remote = _globals.Repository.CreateTfsRemote(new RemoteInfo
                    {
                        Id = remoteId,
                        Url = changeset.Remote.TfsUrl,
                        Repository = changeset.Remote.TfsRepositoryPath,
                        RemoteOptions = _remoteOptions,
                    }, string.Empty);
                remote.UpdateTfsHead(changeset.GitCommit, changeset.ChangesetId);
                _stdout.WriteLine("-> new remote '" + remote.Id + "'");
            }
            else
            {
                remote = changeset.Remote;
                if (changeset.Remote.MaxChangesetId < changeset.ChangesetId)
                {
                    int oldChangeset = changeset.Remote.MaxChangesetId;
                    _globals.Repository.MoveTfsRefForwardIfNeeded(changeset.Remote);
                    _stdout.WriteLine("-> existing remote {0} (updated from changeset {1})", changeset.Remote.Id, oldChangeset);
                }
                else
                {
                    _stdout.WriteLine("-> existing remote {0} (up to date)", changeset.Remote.Id);
                }
            }
            return remote;
        }


        private string GetRemoteId(TfsChangesetInfo changeset)
        {
            if (IsAvailable(GitTfsConstants.DefaultRepositoryId))
            {
                _stdout.WriteLine("info: '" + changeset.Remote.TfsRepositoryPath + "' will be bootstraped as your main remote...");
                return GitTfsConstants.DefaultRepositoryId;
            }

            //Remove '$/'!
            var expectedRemoteId = changeset.Remote.TfsRepositoryPath.Substring(2).Trim('/');
            var indexOfSlash = expectedRemoteId.IndexOf('/');
            if (indexOfSlash != 0)
                expectedRemoteId = expectedRemoteId.Substring(indexOfSlash + 1);
            var remoteId = expectedRemoteId.ToGitRefName();
            var suffix = 0;
            while (!IsAvailable(remoteId))
                remoteId = expectedRemoteId + "-" + (suffix++);
            return remoteId;
        }

        private bool IsAvailable(string remoteName)
        {
            return !_globals.Repository.HasRemote(remoteName);
        }
    }
}
