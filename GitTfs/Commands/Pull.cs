using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    //#todo
    //Figure out Refspecs and figure out if we need to support them. My intuition is no since we are pulling from TFS which has no concept of ref.
    //By spec http://www.kernel.org/pub/software/scm/git/docs/git-pull.html this merges into the current branch. 
    [Pluggable("Pull")]
    [Description("pull [options] tfs-url repository-path")]
    [RequiresValidGitRepository]
    class Pull : GitTfsCommand
    {
        #region GitTfsCommand Members

        public IEnumerable<CommandLine.OptParse.IOptionResults> ExtraOptions
        {
            get { throw new NotImplementedException(); }
        }

        public int Run(IList<string> args)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
