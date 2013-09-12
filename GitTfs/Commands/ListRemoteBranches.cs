using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("list-remote-branches")]
    [Description("list-remote-branches tfs-url-or-instance-name \n       git tfs list-remote-branches http://myTfsServer:8080/tfs/TfsRepository\n")]
    public class ListRemoteBranches : GitTfsCommand
    {
        private readonly Globals globals;
        private readonly ITfsHelper tfsHelper;
        private readonly RemoteOptions remoteOptions;
        private TextWriter stdout;

        public ListRemoteBranches(Globals globals, TextWriter stdout, ITfsHelper tfsHelper, RemoteOptions remoteOptions)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.tfsHelper = tfsHelper;
            this.remoteOptions = remoteOptions;
        }

        public OptionSet OptionSet
        {
            get
            {
                return remoteOptions.OptionSet;
            }
        }

        public int Run(string tfsUrl)
        {
            tfsHelper.Url = tfsUrl;
            tfsHelper.Username = remoteOptions.Username;
            tfsHelper.Password = remoteOptions.Password;
            tfsHelper.EnsureAuthenticated();

            if (!tfsHelper.CanGetBranchInformation)
            {
                throw new GitTfsException("error: this version of TFS doesn't support this functionality");
            }

            var branches = tfsHelper.GetBranches().Where(b => b.IsRoot).ToList();
            if (branches.IsEmpty())
            {
                stdout.WriteLine("No TFS branches were found!");
            }
            else
            {
                stdout.WriteLine("TFS branches that could be cloned:");
                foreach (var branchObject in branches.Where(b => b.IsRoot))
                {
                    Branch.WriteRemoteTfsBranchStructure(tfsHelper, stdout, branchObject.Path);
                }
                stdout.WriteLine("\nCloning root branches (marked by [*]) is recommended!");
                stdout.WriteLine("\n\nPS:if your branch is not listed here, perhaps you should convert the containing folder to a branch in TFS.");
            }
            return GitTfsExitCodes.OK;
        }
    }
}
