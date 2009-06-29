using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        private readonly InitOptions initOptions;
        private readonly RemoteOptions remoteOptions;
        private readonly Globals globals;
        private readonly TextWriter output;

        public Init(RemoteOptions remoteOptions, InitOptions initOptions, Globals globals, TextWriter output)
        {
            this.remoteOptions = remoteOptions;
            this.output = output;
            this.globals = globals;
            this.initOptions = initOptions;
        }

        public bool RequiresValidGitRepository { get { return true; } }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get
            {
                return Helpers.MakeOptionResults(initOptions, remoteOptions);
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
                    output.WriteLine("Invalid arguments to init.");
                    return GitTfsExitCodes.InvalidArguments;
                    
            }
        }

        private void InitSubdir(string repositoryPath)
        {
            var repositoryPath = args.Count == 3 ? args[2] : ".";
            if(!Directory.Exists(repositoryPath))
                Directory.CreateDirectory(repositoryPath);
            Environment.CurrentDirectory = repositoryPath;
            GIT_DIR = ".git";
            repository = git.Repository(GIT_DIR);
        }

        private void DoGitInitDb()
        {
            throw new NotImplementedException();
        }

        private void GitTfsInit(string tfsUrl, string tfsRepositoryPath)
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
