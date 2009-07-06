using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

        public Init(RemoteOptions remoteOptions, InitOptions initOptions, Globals globals, TextWriter output, IGitHelpers gitHelper)
        {
            this.remoteOptions = remoteOptions;
            this.gitHelper = gitHelper;
            this.output = output;
            this.globals = globals;
            this.initOptions = initOptions;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get
            {
                return this.MakeOptionResults(initOptions, remoteOptions);
            }
        }

        public int Run(IList<string> args)
        {
            switch(args.Count)
            {
                case 3:
                    InitSubdir(args[2]);
                    goto case 2;
                case 2:
                    DoGitInitDb();
                    GitTfsInit(args[0], args[1]);
                    return 0;
                default:
                    output.WriteLine("ERROR: Invalid arguments to init.");
                    ObjectFactory.GetInstance<Help>().Run(this);
                    return GitTfsExitCodes.InvalidArguments;
                    
            }
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
                var initCommand = new List<string> {"init"};
                if(initOptions.GitInitTemplate!= null)
                    initCommand.Add("--template=" + initOptions.GitInitTemplate);
                if(initOptions.GitInitShared is string)
                    initCommand.Add("--shared=" + initOptions.GitInitShared);
                else if(initOptions.GitInitShared != null)
                    initCommand.Add("--shared");
                gitHelper.CommandNoisy(initCommand.ToArray());
                globals.Repository = gitHelper.MakeRepository(".git");
            }
        }

        private void GitTfsInit(string tfsUrl, string tfsRepositoryPath)
        {
            // git-svn does this, but I don't know if I want to or not.
            //CommandNoisy("config", "core.autocrlf", "false");
            // TODO - check that there's not already a repository configured with this ID.
            SetConfig("url", tfsUrl);
            SetConfig("repository", tfsRepositoryPath);
            SetConfig("fetch", "refs/remotes/" + globals.RemoteId + "/master");
            if (initOptions.NoMetaData) SetConfig("no-meta-data", 1);
            if (remoteOptions.Username != null) SetConfig("username", remoteOptions.Username);
            if (remoteOptions.IgnoreRegex != null) SetConfig("ignore-paths", remoteOptions.IgnoreRegex);

            Directory.CreateDirectory(Path.Combine(globals.GitDir, "tfs"));
        }

        private void SetConfig(string subkey, object value)
        {
            gitHelper.CommandNoisy("config", globals.RemoteConfigKey(subkey), value.ToString());
        }
    }
}
