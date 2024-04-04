using System.ComponentModel;
using GitTfs.Core;
using StructureMap;

namespace GitTfs.Commands
{
    [Pluggable("checkin")]
    [Description("checkin [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class Checkin : CheckinBase
    {
        public Checkin(CheckinOptions checkinOptions, TfsWriter writer)
            : base(checkinOptions, writer)
        {
        }

        protected override int DoCheckin(TfsChangesetInfo changeset, string refToCheckin) => changeset.Remote.Checkin(refToCheckin, changeset, _checkinOptions);
    }
}
