using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    public abstract class BaseCheckin : BaseTfsCommit
    {
        protected BaseCheckin(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
            : base(globals, stdout, checkinOptions)
        {}

        protected override bool ShouldShowHelp(int argCount)
        {
            return argCount > 1;
        }

        protected override string GetRefToShelve(IList<string> args)
        {
            return args.Any() ? args[0] : base.GetRefToShelve(args);
        }

        protected override int ExecuteCommit(TfsChangesetInfo changeset, string refToShelve, IList<string> args)
        {
            var newChangeset = ExecuteCheckin(changeset.Remote, refToShelve, changeset);
            Stdout.WriteLine("TFS Changeset #" + newChangeset + " was created. Marking it as a merge commit...");
            changeset.Remote.Fetch(new Dictionary<long, string> { { newChangeset, refToShelve } });
            return base.ExecuteCommit(changeset, refToShelve, args);
        }

        protected abstract long ExecuteCheckin(IGitTfsRemote remote, string treeish, TfsChangesetInfo parentChangeset);
    }
}
