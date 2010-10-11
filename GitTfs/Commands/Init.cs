﻿using System;
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
                    return Help.ShowHelpForInvalidArguments(this);
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
            SetConfig("core.autocrlf", "false");
            // TODO - check that there's not already a repository configured with this ID.
            SetTfsConfig("url", tfsUrl);
            SetTfsConfig("repository", tfsRepositoryPath);
            SetTfsConfig("fetch", "refs/remotes/" + globals.RemoteId + "/master");
            if (initOptions.NoMetaData) SetTfsConfig("no-meta-data", 1);
            if (remoteOptions.IgnoreRegex != null) SetTfsConfig("ignore-paths", remoteOptions.IgnoreRegex);

            Directory.CreateDirectory(Path.Combine(globals.GitDir, "tfs"));
        }

        private void SetTfsConfig(string subkey, object value)
        {
            SetConfig(globals.RemoteConfigKey(subkey), value);
        }

        private void SetConfig(string configKey, object value)
        {
            gitHelper.CommandNoisy("config", configKey, value.ToString());
        }
    }
}
