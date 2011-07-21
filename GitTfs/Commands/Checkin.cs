using System.ComponentModel;
using System.IO;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkin")]
    [Description("checkin [options] [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Checkin : CheckinBase
    {
        public Checkin(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer) : base (stdout, checkinOptions, writer)
        {
        }

        protected override long DoCheckin(TfsChangesetInfo changeset, string refToCheckin)
        {
            if (!changeset.Remote.Tfs.CanPerformGatedCheckin && _checkinOptions.QueueBuildForGatedCheckIn)
                throw new GitTfsException(
                    "gated checkin does not work with this TFS version (" + changeset.Remote.Tfs.TfsClientLibraryVersion + ").",
                    new[] { "Try installing the VS2010 edition of Team Explorer." });

            return changeset.Remote.Checkin(refToCheckin, changeset);
        }
    }
}
