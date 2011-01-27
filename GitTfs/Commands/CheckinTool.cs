using System.Collections.Generic;
using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [PluggableWithAliases("checkintool", "ct")]
    [Description("checkintool [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class CheckinTool : GitTfsCommand
    {
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        public CheckinTool(CheckinOptions checkinOptions, TfsWriter writer)
        {
            _checkinOptions = checkinOptions;
            _writer = writer;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(_checkinOptions); }
        }

        public int Run()
        {
            return Run("HEAD");
        }

        public int Run(string refToCheckin)
        {
            return _writer.Write(refToCheckin, changeset =>
            {
                if (changeset.Remote.Tfs.CanShowCheckinDialog)
                    changeset.Remote.CheckinTool(refToCheckin, changeset);
                else
                    throw new GitTfsException(
                        "checkintool does not work with this TFS version (" + changeset.Remote.Tfs.TfsClientLibraryVersion + ").",
                        new[] {"Try installing the VS2010 edition of Team Explorer."});
                return GitTfsExitCodes.OK;
            });
        }
    }
}
