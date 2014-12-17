using System;
using System.Collections.Generic;
using System.Linq;

using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Util
{
    /// <summary>
    /// Uses the IDisposable pattern to wrap a block of code in `git stash` and `git stash pop`
    /// </summary>
    public class TemporaryStash : IDisposable
    {
        private readonly IGitRepository _repo;
        private readonly bool _workingCopyWasDirty;

        public TemporaryStash(IGitRepository repo)
        {
            if (repo == null) throw new ArgumentNullException("repo");

            _repo = repo;
            _workingCopyWasDirty = _repo.WorkingCopyHasUnstagedOrUncommitedChanges;

            Stash();
        }

        private void Stash()
        {
            if (_workingCopyWasDirty)
                _repo.CommandNoisy("stash");
        }

        void IDisposable.Dispose()
        {
            if (_workingCopyWasDirty)
                _repo.CommandNoisy("stash", "pop");
        }
    }
}
