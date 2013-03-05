using System.ComponentModel;
using System.IO;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("quick-clone")]
    [Description("quick-clone [options] tfs-url-or-instance-name repository-path <git-repository-path>")]
    public class QuickClone : Clone
    {
        public QuickClone(Globals globals, Init init, QuickFetch fetch, TextWriter stdout)
            : base(globals, fetch, init, null, stdout)
        {
        }
    }
}
