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
    [Description(@"create [options] tfs-url-or-instance-name project-name -t=trunk-name <git-repository-path>
ex : git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName
     git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName -t=myTrunkName
")]
    public class Create : GitTfsCommand
    {
        private readonly Clone _clone;
        private readonly TextWriter _stdout;
        private readonly ITfsHelper _tfsHelper;
        private readonly RemoteOptions _remoteOptions;
        private string _trunkName = "trunk";
        private bool _createTeamProjectFolder;

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
                return new OptionSet
                    {
                        {"c|create-project-folder", "Create also the team project folder if it doesn't exist!", v => _createTeamProjectFolder = v != null},
                        {"t|trunk-name=", "name of the main branch that will be created on TFS (default: \"trunk\")", v => _trunkName = v},
                    }.Merge(_clone.OptionSet);
            }
        }

        public int Run(string tfsUrl, string projectName)
        {
            return Run(tfsUrl, projectName, Path.GetFileName(projectName));
        }

        public int Run(string tfsUrl, string projectName, string gitRepositoryPath)
        {
            _tfsHelper.Url = tfsUrl;
            _tfsHelper.Username = _remoteOptions.Username;
            _tfsHelper.Password = _remoteOptions.Password;

            var absoluteGitRepositoryPath = Path.GetFullPath(gitRepositoryPath);
            _stdout.WriteLine("Creating project folder...");
            _tfsHelper.CreateTfsRootBranch(projectName, _trunkName, absoluteGitRepositoryPath, _createTeamProjectFolder);
            _stdout.WriteLine("Cloning new project...");
            _clone.Run(tfsUrl, "$/" + projectName + "/" + _trunkName, gitRepositoryPath);

            return GitTfsExitCodes.OK;
        }
    }
}
