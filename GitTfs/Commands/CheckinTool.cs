using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [PluggableWithAliases("checkintool", "ct")]
    [Description("checkintool [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class CheckinTool : BaseCheckin
    {
        public CheckinTool(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
            : base(globals, stdout, checkinOptions)
        {}

        protected override long ExecuteCheckin(IGitTfsRemote remote, string treeish, TfsChangesetInfo parentChangeset)
        {
            return remote.CheckinTool(treeish, parentChangeset);
        }
    }
}
