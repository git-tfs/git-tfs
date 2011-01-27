using System;
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
    [Description("checkin [options] [ref-to-shelve]")]
    [RequiresValidGitRepository]
    public class Checkin : GitTfsCommand
    {
        private readonly TextWriter _stdout;
        private readonly CheckinOptions _checkinOptions;
        private readonly TfsWriter _writer;

        public Checkin(TextWriter stdout, CheckinOptions checkinOptions, TfsWriter writer)
        {
            _writer = writer;
            _stdout = stdout;
            _checkinOptions = checkinOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(_checkinOptions); }
        }

        // TODO: DRY up, w.r.t. with Shelve.
        public int Run()
        {
            return Run("HEAD");
        }

        public int Run(string refToCheckin)
        {
            return _writer.Write(refToCheckin, changeset =>
            {
                var newChangeset = changeset.Remote.Checkin(refToCheckin, changeset);
                _stdout.WriteLine("TFS Changeset #" + newChangeset + " was created. Marking it as a merge commit...");
                changeset.Remote.Fetch(new Dictionary<long, string> { { newChangeset, refToCheckin } });
                return GitTfsExitCodes.OK;
            });
        }
    }
}
