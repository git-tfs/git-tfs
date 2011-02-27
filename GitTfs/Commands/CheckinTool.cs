using System.ComponentModel;
using System.IO;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [PluggableWithAliases("checkintool", "ct")]
    [Description("checkintool [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class CheckinTool : CheckinBase
    {
        public CheckinTool(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer) : base(stdout, checkinOptions, writer)
        {
        }

        protected override long DoCheckin(TfsChangesetInfo changeset, string refToCheckin)
        {
            if (!changeset.Remote.Tfs.CanShowCheckinDialog)
                throw new GitTfsException(
                    "checkintool does not work with this TFS version (" + changeset.Remote.Tfs.TfsClientLibraryVersion + ").",
                    new[] {"Try installing the VS2010 edition of Team Explorer."});

            return changeset.Remote.CheckinTool(refToCheckin, changeset);
        }
    }
}
