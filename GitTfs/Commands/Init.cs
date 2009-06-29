using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CommandLine.OptParse;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("init")]
    [Description("init [options] tfs-url repository-path [git-repository]")]
    public class Init : GitTfsCommand
    {
        //private GitTfs gitTfs;
        private InitOptions initOptions;
        private RemoteOptions remoteOptions;

        public IEnumerable<ParseHelper> ExtraOptions
        {
            get
            {
                return from options in new [] { initOptions, remoteOptions }
                       select new PropertySomethingParseHelper(options);
            }
        }

        public Init()
        {
            id = GitTfsConstants.DefaultRemoteId;
        }

        public int Run(IEnumerable<string> args)
        {
            InitSubdir(args);
            GitTfsInit(args);
            return 0;
        }

        private void InitSubdir(IList<string> args)
        {
            var repositoryPath = args.Count == 3 ? args[2] : ".";
            if(!Directory.Exists(repositoryPath))
                Directory.CreateDirectory(repositoryPath);
            Environment.ChangeDirectory(repositoryPath);
            GIT_DIR = ".git";
            repository = git.Repository(GIT_DIR);
        }

        private void GitTfsInit(IList<string> args)
        {
            if(!Directory.Exists(GIT_DIR))
            {
                var initArgs = new List<string>();
                initArgs.Add("init");
                if(template != null)
                {
                    initArgs.Add("--template=" + template);
                }
                if(shared != null)
                {
                    if(shared is String)
                    {
                        initArgs.Add("--shared=" + shared);
                    }
                    else
                    {
                        initArgs.Add("--shared");
                    }
                }
                CommandNoisy(initArgs);
                repository = git.Repository(".git");
            }
            // git-svn does this, but I don't know if I want to or not.
            //CommandNoisy("config", "core.autocrlf", "false");
            var prefix = "tfs-remote." + id;
            if(NoMetaData)  CommandNoisy("config", prefix + ".no-meta-data", 1);
            if(username)    CommandNoisy("config", prefix + ".username", username);
            if(IgnoreRegex) CommandNoisy("config", prefix + ".ignore-paths", IgnoreRegex);
            CommandNoisy("config", prefix + ".url" + args[0]);
            CommandNoisy("config", prefix + ".repository" + args[1]);
        }
    }
}
