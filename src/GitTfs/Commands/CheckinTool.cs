using System.ComponentModel;
using GitTfs.Core;
using GitTfs.Util;

namespace GitTfs.Commands
{
    [PluggableWithAliases("checkintool", "ct")]
    [Description("checkintool [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class CheckinTool : CheckinBase
    {
        public CheckinTool(CheckinOptions checkinOptions, TfsWriter writer) : base(checkinOptions, writer)
        {
        }

        protected override int DoCheckin(TfsChangesetInfo changeset, string refToCheckin)
        {
            if (!changeset.Remote.Tfs.CanShowCheckinDialog)
                throw new GitTfsException(
                    "checkintool does not work with this TFS version (" + changeset.Remote.Tfs.TfsClientLibraryVersion + ").",
                    new[] {
                        "Try installing the Team Explorer matching the TFS client libraries",
                        "Alternatively, set the GIT_TFS_CLIENT environment variable to a supported/installed Visual Studio version",
                        });

            return changeset.Remote.CheckinTool(refToCheckin, changeset);
        }
    }
}
