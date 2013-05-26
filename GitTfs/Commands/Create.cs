using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;
using Sep.Git.Tfs.Util;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("create")]
    [Description(@"create [options] tfs-url-or-instance-name project-name <trunk-name> <git-repository-path>
ex : git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName myTrunkName
if 'project-name' doesn't exist it will be created
if 'trunk-name' is not specified, the default value is 'trunk'")]
    public class Create : GitTfsCommand
    {
        private readonly Clone _clone;
        private TextWriter _stdout;
        private readonly ITfsHelper _tfsHelper;
        private readonly RemoteOptions _remoteOptions;

        public Create(ITfsHelper tfsHelper, Clone clone, RemoteOptions remoteOptions, TextWriter stdout)
        {
            _stdout = stdout;
            _tfsHelper = tfsHelper;
            _clone = clone;
            _remoteOptions = remoteOptions;
        }

        public OptionSet OptionSet
        {
            get
            {
                return _clone.OptionSet;
            }
        }

        public int Run(string tfsUrl, string projectName)
        {
            return Run(tfsUrl, projectName, "trunk");
        }

        public int Run(string tfsUrl, string projectName, string mainBranch)
        {
            return Run(tfsUrl, projectName, mainBranch, ".");
        }

        public int Run(string tfsUrl, string projectName, string mainBranch, string gitRepositoryPath)
        {
            _tfsHelper.Url = tfsUrl;
            _tfsHelper.Username = _remoteOptions.Username;
            _tfsHelper.Password = _remoteOptions.Password;

            var absoluteGitRepositoryPath = Path.GetFullPath(gitRepositoryPath);
            _stdout.WriteLine("Creating project folder...");
            _tfsHelper.CreateTfsRootBranch(projectName, mainBranch, absoluteGitRepositoryPath);
            _stdout.WriteLine("Cloning new project...");
            _clone.Run(tfsUrl, "$/" + projectName + "/" + mainBranch, gitRepositoryPath);

            return GitTfsExitCodes.OK;
        }
    }
}
