using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("init")]
    [Description("init [options] tfs-url repository-path [git-repository]")]
    public class Init : GitTfsCommand
    {
        private readonly InitOptions initOptions;
        private readonly RemoteOptions remoteOptions;
        private readonly Globals globals;
        private readonly TextWriter output;
        private readonly IGitHelpers gitHelper;
        private readonly IHelpHelper _help;

        public Init(RemoteOptions remoteOptions, InitOptions initOptions, Globals globals, TextWriter output, IGitHelpers gitHelper, IHelpHelper help)
        {
            this.remoteOptions = remoteOptions;
            this.gitHelper = gitHelper;
            _help = help;
            this.output = output;
            this.globals = globals;
            this.initOptions = initOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get
            {
                return this.MakeNestedOptionResults(initOptions, remoteOptions);
            }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            tfsRepositoryPath.AssertValidTfsPath();
            DoGitInitDb();
            GitTfsInit(tfsUrl, tfsRepositoryPath);
            return 0;
        }

        public int Run(string tfsUrl, string tfsRepositoryPath, string gitRepositoryPath)
        {
            tfsRepositoryPath.AssertValidTfsPath();
            InitSubdir(gitRepositoryPath);
            return Run(tfsUrl, tfsRepositoryPath);
        }

        private void InitSubdir(string repositoryPath)
        {
            if(!Directory.Exists(repositoryPath))
                Directory.CreateDirectory(repositoryPath);
            Environment.CurrentDirectory = repositoryPath;
            globals.GitDir = ".git";
            globals.Repository = gitHelper.MakeRepository(globals.GitDir);
        }

        private void DoGitInitDb()
        {
            if(!Directory.Exists(globals.GitDir))
            {
                gitHelper.CommandNoisy(BuildInitCommand());
                globals.Repository = gitHelper.MakeRepository(".git");
            }
        }

        private string[] BuildInitCommand()
        {
            var initCommand = new List<string> {"init"};
            if(initOptions.GitInitTemplate!= null)
                initCommand.Add("--template=" + initOptions.GitInitTemplate);
            if(initOptions.GitInitShared is string)
                initCommand.Add("--shared=" + initOptions.GitInitShared);
            else if(initOptions.GitInitShared != null)
                initCommand.Add("--shared");
            return initCommand.ToArray();
        }

        private void GitTfsInit(string tfsUrl, string tfsRepositoryPath)
        {
            gitHelper.SetConfig("core.autocrlf", "false");
            globals.Repository.CreateTfsRemote(globals.RemoteId, tfsUrl, tfsRepositoryPath, remoteOptions);
        }
    }

    public static partial class Ext
    {
        static Regex ValidTfsPath = new Regex("^\\$/.+");
        public static void AssertValidTfsPath(this string tfsPath)
        {
            if (!ValidTfsPath.IsMatch(tfsPath))
                throw new GitTfsException("TFS repository can not be root and must start with \"$/\".", SuggestPaths(tfsPath));
        }

        private static IEnumerable<string> SuggestPaths(string tfsPath)
        {
            if (tfsPath == "$" || tfsPath == "$/")
                yield return "Cloning an entire TFS repository is not supported. Try using a subdirectory of the root (e.g. $/MyProject).";
            else if (tfsPath.StartsWith("$"))
                yield return "Try using $/" + tfsPath.Substring(1);
            else
                yield return "Try using $/" + tfsPath;
        }
    }
}
