using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkin")]
    [Description("checkin [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class Checkin : BaseCheckin
    {
        public Checkin(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
            : base(globals, stdout, checkinOptions)
        {}

        protected override long ExecuteCheckin(IGitTfsRemote remote, string treeish, TfsChangesetInfo parentChangeset)
        {
            return remote.Checkin(treeish, parentChangeset);
        }
    }
}
