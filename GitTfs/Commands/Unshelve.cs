using System.ComponentModel;
using System.IO;
using System.Linq;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("unshelve")]
    [Description("unshelve [options] shelve-name destination-branch")]
    [RequiresValidGitRepository]
    public class Unshelve : GitTfsCommand
    {
        private readonly Globals _globals;
        private readonly TextWriter _stdout;

        public Unshelve(Globals globals, TextWriter stdout)
        {
            _globals = globals;
            _stdout = stdout;
            TfsBranch = null;
        }

        public string Owner { get; set; }
        public string TfsBranch { get; set; }

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "u|user=", "Shelveset owner (default: current user)\nUse 'all' to search all shelvesets.",
                        v => Owner = v },
                    { "b|branch=", "Git Branch to apply Shelveset to? (default: TFS default branch)", 
                        v => TfsBranch = v },                
                };
            }
        }

        public int Run(string shelvesetName, string destinationBranch)
        {
            if (string.IsNullOrEmpty(TfsBranch))//If destination not on command line, set up defaults.
            {
                TfsBranch = _globals.RemoteId;  //Default to main remote id.
                //Get the current checkout
                TfsChangesetInfo mostRecentUpdate = _globals.Repository.GetLastParentTfsCommits("HEAD").FirstOrDefault();
                if (mostRecentUpdate != null)
                {
                    TfsBranch = mostRecentUpdate.Remote.Id;
                }
            }

            var remote = _globals.Repository.ReadTfsRemote(TfsBranch);
            remote.Unshelve(Owner, shelvesetName, destinationBranch);
            _stdout.WriteLine("Created branch " + destinationBranch + " from shelveset \"" + shelvesetName + "\".");
            return GitTfsExitCodes.OK;
        }
    }
}
