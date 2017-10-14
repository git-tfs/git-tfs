using System.ComponentModel;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkin")]
    [Description("checkin [options] [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Checkin : CheckinBase
    {
        public Checkin(CheckinOptions checkinOptions, TfsWriter writer)
            : base(checkinOptions, writer)
        {
        }

        protected override int DoCheckin(TfsChangesetInfo changeset, string refToCheckin)
        {
            return changeset.Remote.Checkin(refToCheckin, changeset, _checkinOptions);
        }
    }
}
