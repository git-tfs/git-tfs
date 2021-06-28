using System.ComponentModel;
using System.Linq;
using NDesk.Options;
using StructureMap;
using GitTfs.Core;
using GitTfs.Core.TfsInterop;
using System.Diagnostics;

namespace GitTfs.Commands
{
    [Pluggable("list-remote-branches")]
    [Description("list-remote-branches tfs-url-or-instance-name \n       git tfs list-remote-branches http://myTfsServer:8080/tfs/TfsRepository\n")]
    public class ListRemoteBranches : GitTfsCommand
    {
        private readonly ITfsHelper _tfsHelper;
        private readonly RemoteOptions _remoteOptions;

        public ListRemoteBranches(ITfsHelper tfsHelper, RemoteOptions remoteOptions)
        {
            _tfsHelper = tfsHelper;
            _remoteOptions = remoteOptions;
        }

        public OptionSet OptionSet
        {
            get
            {
                return _remoteOptions.OptionSet;
            }
        }

        public int Run(string tfsUrl)
        {
            _tfsHelper.Url = tfsUrl;
            _tfsHelper.Username = _remoteOptions.Username;
            _tfsHelper.Password = _remoteOptions.Password;
            _tfsHelper.EnsureAuthenticated();

            string convertBranchMessage = "  -> Open 'Source Control Explorer' and for each folder corresponding to a branch, right click on the folder and select 'Branching and Merging' > 'Convert to branch'.";
            var branches = _tfsHelper.GetBranches().Where(b => b.IsRoot).ToList();
            if (branches.IsEmpty())
            {
                Trace.TraceWarning("No TFS branches were found!");
                Trace.TraceWarning("\n\nPerhaps you should convert your branch folders into a branch in TFS:");
                Trace.TraceWarning(convertBranchMessage);
            }
            else
            {
                Trace.TraceInformation("TFS branches that could be cloned:");
                foreach (var branchObject in branches.Where(b => b.IsRoot))
                {
                    Branch.WriteRemoteTfsBranchStructure(_tfsHelper, branchObject.Path);
                }
                Trace.TraceInformation("\nCloning root branches (marked by [*]) is recommended!");
                Trace.TraceInformation("\n\nPS:if your branch is not listed here, perhaps you should convert its containing folder into a branch in TFS:");
                Trace.TraceInformation(convertBranchMessage);
            }
            return GitTfsExitCodes.OK;
        }
    }
}
